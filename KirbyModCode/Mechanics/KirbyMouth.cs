using KirbyMod.Cards;
using KirbyMod.Characters;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;
using Godot;
using MegaCrit.Sts2.Core.Combat;

namespace KirbyMod.Mechanics;

/// <summary>Shared rules for Inhale, Mouthful, Spit Star, Swallow and Drop Ability (DESIGN.md, 核心卡牌 / 状态).</summary>
public static class KirbyMouth
{
    /// <summary>Mouthful when the inhaled enemy does something other than attack.</summary>
    public const int FallbackMouthful = 6;
    /// <summary>An enemy at or below this HP when inhaled is swallowed whole (dies on the spot).</summary>
    public const int InhaleExecuteThreshold = 16;
    /// <summary>The same for Hypernova Inhale.</summary>
    public const int FrenziedExecuteThreshold = 33;

    /// <summary>Inhale cards can't be played while Kirby holds a copy ability, unless he has Enhanced Inhale.</summary>
    public static bool CanUseInhale(Player? player) =>
        player == null || !CopyAbilities.HasAny(player) || player.Creature.HasPower<EnhancedInhalePower>();

    /// <summary>An act boss itself (not its minions) can't be swallowed whole.</summary>
    public static bool IsActBoss(Creature creature) =>
        creature.CombatState?.Encounter?.RoomType == RoomType.Boss && creature.IsPrimaryEnemy;

    public static readonly PileType[] LivePiles = [PileType.Hand, PileType.Draw, PileType.Discard];

    public static int MouthfulOf(Creature kirby) => kirby.GetPower<MouthfulPower>()?.Amount ?? 0;

    public static async Task StartInhale(PlayerChoiceContext ctx, Player player, Creature target, CardModel source)
    {
        var kirby = player.Creature;
        var (readKind, outcome) = CopyAbilities.ReadIntent(target);
        // Unknown intent: a random ability.
        var kind = readKind ?? player.RunState.Rng.CombatCardGeneration.NextItem(CopyAbilities.All.ToList());
        Entry.Logger.Info($"[Inhale] {target.Monster?.Id.Entry} move={target.Monster?.NextMove.Id} " +
                          $"hp={target.CurrentHp}/{target.MaxHp} -> ability={kind}{(readKind == null ? " (random)" : "")}, outcome={outcome}");

        KirbyFx.PlaySound(source is Scan ? KirbyFx.ScanSound : KirbyFx.InhaleSound);
        VfxCmd.PlayOnCreatureCenter(target, "vfx/vfx_gaseous_impact");

        // At or below the threshold (33 for Hypernova Inhale) the enemy is swallowed whole: it dies at the end of this
        // method, and Inhaling's death reward (half its Max HP as Mouthful) is the success. Not act bosses.
        var threshold = source is UltimateInhale ? FrenziedExecuteThreshold : InhaleExecuteThreshold;
        var swallow = target.IsAlive && target.CurrentHp <= threshold && !IsActBoss(target);

        // Escaping enemy: stunned so it stays, then a normal inhale with the fallback Mouthful and Wheel in the mouth.
        if (outcome == InhaleOutcome.Escape && !swallow)
        {
            await CreatureCmd.Stun(target);
            outcome = InhaleOutcome.Fallback;
        }

        await PowerCmd.Remove(kirby.GetPower<InhalingPower>());
        var inhaling = (InhalingPower)ModelDb.Power<InhalingPower>().ToMutable();
        inhaling.Begin(target, outcome, upgradedTokens: source.IsUpgraded, negateAll: source is UltimateInhale);
        await PowerCmd.Apply(ctx, inhaling, kirby, 1, kirby, source);

        // A new Inhale overrides whatever ability was already in Kirby's mouth. No-Ability Inhale leaves it empty.
        await PowerCmd.Remove(kirby.GetPower<AbilityInMouthPower>());
        if (source is not GooeyInhale)
        {
            var inMouth = (AbilityInMouthPower)ModelDb.Power<AbilityInMouthPower>().ToMutable();
            inMouth.Kind = kind;
            await PowerCmd.Apply(ctx, inMouth, kirby, 1, kirby, source);
        }

        if (swallow)
        {
            Entry.Logger.Info($"[Inhale] {target.Monster?.Id.Entry} swallowed whole (hp {target.CurrentHp} <= {threshold})");
            await SwallowWhole(kirby, target);
        }
    }

    /// <summary>
    /// Kills <paramref name="target" />, first flying it into Kirby while it shrinks to 80%, instead of its death
    /// animation (the way Doom makes enemies disappear). Enemies that revive or have their own death sequence (the
    /// game's ShouldDisappearFromDoom list: Test Subject before its last life, illusions, Decimillipede segments, ...)
    /// keep their normal death.
    /// </summary>
    private static async Task SwallowWhole(Creature kirby, Creature target)
    {
        var room = NCombatRoom.Instance;
        var node = room?.GetCreatureNode(target);
        var kirbyNode = room?.GetCreatureNode(kirby);

        // Wind: Whirlwind's whole-screen streaks, blowing towards Kirby (leftwards).
        room?.CombatVfxContainer.AddChildSafely(NHorizontalLinesVfx.Create(new Color("FFFFFF80"), 1.0,
            movingRightwards: false));

        if (room != null && node != null && kirbyNode != null && target.Monster?.ShouldDisappearFromDoom == true)
        {
            const float duration = 0.7f;
            const float endScale = 0.8f;
            node.Hitbox.ReleaseFocus();
            node.Hitbox.FocusMode = Control.FocusModeEnum.None;
            node.Hitbox.MouseFilter = Control.MouseFilterEnum.Ignore;
            // No health bar, powers, name or intents on the flying body. Both are scene nodes in 0.107 and 0.113
            // (the game's own hide/animate-out methods differ between the two).
            var stateDisplay = node.GetNodeOrNull<CanvasItem>("%HealthBar");
            if (stateDisplay != null) stateDisplay.Visible = false;
            node.IntentContainer.Visible = false;

            // Body centres = hitbox centres (Kirby's hitbox centre is the middle of his sprite). Each frame the
            // enemy's own centre is put on the path, whatever its scale and rotation, so the half turn cannot throw
            // it off the line (PivotOffset did not hold: the body swung round its feet and landed below Kirby).
            var from = node.Hitbox.GetGlobalRect().GetCenter();
            var to = kirbyNode.Hitbox.GetGlobalRect().GetCenter();
            var parentInverse = (node.GetParent() as CanvasItem)?.GetGlobalTransform().AffineInverse()
                ?? Transform2D.Identity;
            var centre = node.GetGlobalTransform().AffineInverse() * from;  // in the enemy's own (local) space
            var startScale = node.Scale;
            var startRotation = node.Rotation;
            node.PivotOffset = Vector2.Zero;

            void Step(float t)
            {
                var path = t * t * t * t;  // slow start, then sucked in fast (quartic ease-in)
                var turn = t * t;          // shrinking and the half turn speed up too, a little less sharply
                var scale = startScale.Lerp(startScale * endScale, turn);
                var rotation = startRotation + Mathf.Pi * turn;
                node.Scale = scale;
                node.Rotation = rotation;
                var body = new Transform2D(rotation, scale, 0f, Vector2.Zero) * centre;
                node.Position = parentInverse * from.Lerp(to, path) - body;
            }

            var tween = node.CreateTween();
            tween.TweenMethod(Callable.From<float>(Step), 0f, 1f, duration);
            tween.TweenCallback(Callable.From(node.QueueFree));

            // Like Doom: the node leaves the room now, so the kill below finds no node and plays no death animation.
            node.DeathAnimationTask = WaitFor(node, tween);
            room.RemoveCreatureNode(node);
            await Cmd.Wait(duration);
        }

        await CreatureCmd.Kill(target);
    }

    private static async Task WaitFor(Node owner, Tween tween) =>
        await owner.ToSignal(tween, Tween.SignalName.Finished);

    /// <summary>
    /// Inhale succeeded: gain Mouthful and get Spit Star and Swallow (one of each in hand, upgraded if the Inhale was).
    /// </summary>
    public static async Task Succeed(PlayerChoiceContext ctx, Creature kirby, int mouthful, bool upgradedTokens,
        string reason)
    {
        var player = kirby.Player;
        if (player == null || kirby.IsDead)
            return;

        var before = MouthfulOf(kirby);
        await PowerCmd.Apply<MouthfulPower>(ctx, kirby, Math.Max(mouthful, 0), kirby, null);
        // Inhaling lasts until Kirby's next turn, but the mouth is already full. Switched off only after Mouthful is on,
        // so the sprite goes straight from inhaling to full mouth (not through the normal one).
        // The last enemy dying ends the combat, and the game then applies no more powers: Kirby still shows the full
        // mouth he earned for the rest of the fight's ending, with the Mouthful sound (normally played by MouthfulPower).
        if (CombatManager.Instance.IsEnding && mouthful > 0)
        {
            KirbySprite.SetMouthful(kirby, true);
            KirbyFx.PlaySound(KirbyFx.MouthfulSound, whileCombatEnds: true);
        }
        KirbySprite.SetInhaling(kirby, false);
        Entry.Logger.Info($"[Inhale] success ({reason}): Mouthful {before} + {mouthful} -> {MouthfulOf(kirby)}");
        await AddToHandIfMissing<SpitStar>(player, upgradedTokens);
        await AddToHandIfMissing<Swallow>(player, upgradedTokens);
    }

    public static async Task ClearMouth(Creature kirby, bool keepMouthful = false)
    {
        if (!keepMouthful)
            await PowerCmd.Remove(kirby.GetPower<MouthfulPower>());
        await PowerCmd.Remove(kirby.GetPower<AbilityInMouthPower>());
    }

    /// <summary>Spit Star and Swallow are a pair: once either is played, every other copy of both is exhausted.</summary>
    public static async Task ExhaustOtherMouthCards(PlayerChoiceContext ctx, Player player, CardModel played)
    {
        var others = LivePiles
            .SelectMany(pile => pile.GetPile(player).Cards)
            .Where(card => card != played && card is SpitStar or Swallow)
            .ToList();

        foreach (var card in others)
            await CardCmd.Exhaust(ctx, card);
    }

    public static CardModel? FindExhaustedInhale(Player player) =>
        PileType.Exhaust.GetPile(player).Cards.FirstOrDefault(card => card is IInhaleCard);

    /// <summary>
    /// Moves Inhale into hand: from the exhaust pile, or with <paramref name="includeLivePiles" /> also from the draw
    /// and discard piles (Enhanced Inhale). Returns whether a card moved.
    /// </summary>
    public static async Task<bool> PutInhaleIntoHand(Player player, bool includeLivePiles = false)
    {
        var inhale = FindExhaustedInhale(player);
        if (inhale == null && includeLivePiles)
        {
            inhale = new[] { PileType.Draw, PileType.Discard }
                .SelectMany(pile => pile.GetPile(player).Cards)
                .FirstOrDefault(card => card is IInhaleCard);
        }

        if (inhale == null)
            return false;

        await CardPileCmd.Add(inhale, PileType.Hand);
        return true;
    }

    /// <summary>
    /// Removes one ability (Sleep waking up). The cards it handed out stay (they are Ethereal and Exhaust anyway);
    /// Drop Ability is exhausted once no ability is left.
    /// </summary>
    public static async Task LoseAbility(PlayerChoiceContext ctx, Player player, CopyAbilityPower power)
    {
        await power.OnLost(ctx, player);
        await PowerCmd.Remove(power);

        if (!CopyAbilities.HasAny(player))
            await ExhaustDropAbility(ctx, player);
    }

    /// <summary>Removes every copy ability and exhausts Drop Ability. Returns whether anything was lost.</summary>
    public static async Task<bool> LoseCopyAbility(PlayerChoiceContext ctx, Player player)
    {
        var powers = player.Creature.Powers.OfType<CopyAbilityPower>().ToList();
        foreach (var power in powers)
        {
            await power.OnLost(ctx, player);
            await PowerCmd.Remove(power);
        }

        var dropped = await ExhaustDropAbility(ctx, player);
        return powers.Count > 0 || dropped;
    }

    private static async Task<bool> ExhaustDropAbility(PlayerChoiceContext ctx, Player player)
    {
        var cards = LivePiles.SelectMany(pile => pile.GetPile(player).Cards).OfType<DropAbility>().ToList();
        foreach (var card in cards)
            await CardCmd.Exhaust(ctx, card);
        return cards.Count > 0;
    }

    /// <summary>
    /// Drop Ability, Great Cave Offensive: lose the ability on purpose (the starter relic then returns Inhale). Void
    /// cannot be dropped: you get the do-nothing Ability Lock instead. Returns whether an ability was dropped.
    /// </summary>
    public static async Task<bool> DropCopyAbility(PlayerChoiceContext ctx, Player player)
    {
        if (await BlockedByVoid(ctx, player))
            return false;

        if (!await LoseCopyAbility(ctx, player))
            return false;
        await CopyAbilities.AfterAbilityLost(player);
        return true;
    }

    /// <summary>True (and Ability Lock applied) when <paramref name="player" /> holds Void, which cannot be given up or changed.</summary>
    public static async Task<bool> BlockedByVoid(PlayerChoiceContext ctx, Player player)
    {
        var kirby = player.Creature;
        if (!kirby.HasPower<VoidPower>())
            return false;

        if (!kirby.HasPower<AbilityLockPower>())
            await PowerCmd.Apply<AbilityLockPower>(ctx, kirby, 1, kirby, null);
        return true;
    }

    /// <summary>Adds a <typeparamref name="TCard" /> to hand unless there is one; an <paramref name="upgraded" /> request upgrades the one there.</summary>
    public static async Task AddToHandIfMissing<TCard>(Player player, bool upgraded = false) where TCard : CardModel
    {
        var existing = PileType.Hand.GetPile(player).Cards.FirstOrDefault(card => card is TCard);
        if (existing != null)
        {
            if (upgraded && existing.IsUpgradable)
                CardCmd.Upgrade(existing);
            return;
        }

        if (player.Creature.CombatState == null)
            return;

        await CardPileCmd.AddGeneratedCardToCombat(CreateCard<TCard>(player, upgraded), PileType.Hand, player);
    }

    /// <summary>Creates a card in combat, upgraded if asked.</summary>
    public static TCard CreateCard<TCard>(Player player, bool upgraded) where TCard : CardModel
    {
        var card = player.Creature.CombatState!.CreateCard<TCard>(player);
        if (upgraded && card.IsUpgradable)
            CardCmd.Upgrade(card);
        return card;
    }

    public static CardModel CreateCard(Type type, Player player, bool upgraded)
    {
        var card = player.Creature.CombatState!.CreateCard(ModelDb.GetById<CardModel>(ModelDb.GetId(type)), player);
        if (upgraded && card.IsUpgradable)
            CardCmd.Upgrade(card);
        return card;
    }
}
