using System.Runtime.CompilerServices;
using KirbyMod.Cards;
using KirbyMod.Powers;
using KirbyMod.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace KirbyMod.Mechanics;

public enum CopyAbilityKind
{
    Sword,
    Boomerang,
    Stone,
    Fighter,
    Ice,
    Fire,
    Sleep,
    Plasma,
    Car,
    StarRod,
    Galaxia,
    Doctor,
    Wheel,
}

/// <summary>How a given Inhale can succeed, decided from the target's intent when Inhale is played.</summary>
public enum InhaleOutcome
{
    /// <summary>The target attacks: its first attack on Kirby gives Mouthful equal to the damage of all its hits (they still land).</summary>
    FirstHit,

    /// <summary>The target adds status cards: the first one it gives Kirby is removed.</summary>
    BlockFirstStatusCard,

    /// <summary>Anything else: Inhale resolves with the fallback Mouthful at the start of Kirby's next turn.</summary>
    Fallback,

    /// <summary>The target is about to escape: it is stunned instead; no Mouthful and no ability.</summary>
    Escape,
}

/// <summary>
/// Card types that always belong to a copy ability (Drop Ability); they are exhausted when it is lost.
/// Individual cards an ability hands out are tagged instead (<see cref="CopyAbilities.MarkGranted" />).
/// </summary>
public interface ICopyAbilityCard
{
    /// <summary>The ability whose loss exhausts this card, or null for "once no ability is left" (Drop Ability).</summary>
    CopyAbilityKind? GrantedBy { get; }
}

/// <summary>Inhale, Gooey Inhale and Scan: they start an inhale and count as "Inhale" for other effects.</summary>
public interface IInhaleCard;

/// <summary>Marks cards that gain Retain (and usually a bonus) while Kirby holds one of <see cref="SynergyAbilities" />.</summary>
public interface IAbilitySynergyCard
{
    IReadOnlyList<CopyAbilityKind> SynergyAbilities { get; }
}

public static class CopyAbilities
{
    public static readonly IReadOnlyList<CopyAbilityKind> All = Enum.GetValues<CopyAbilityKind>();

    /// <summary>
    /// Cards an ability hands out when gained: 1 of its lowest-rarity ones, or from an upgraded ability card 1 of its
    /// Uncommon-or-better ones.
    /// </summary>
    private static readonly Dictionary<CopyAbilityKind, Type[]> AbilityCardTypes = new()
    {
        [CopyAbilityKind.Sword] = [typeof(Slash), typeof(SpinSlash), typeof(RisingSlash), typeof(MachTornado)],
        [CopyAbilityKind.Boomerang] = [typeof(BoomerangThrow), typeof(FinalCutter), typeof(RisingSlash)],
        [CopyAbilityKind.Stone] = [typeof(InvincibleGuard), typeof(StonePunch)],
        [CopyAbilityKind.Fighter] = [typeof(ChargeUp), typeof(ForceBlast)],
        [CopyAbilityKind.Ice] = [typeof(IceBreath), typeof(IceBlock)],
        [CopyAbilityKind.Fire] = [typeof(IceBreath), typeof(Burn)],
        [CopyAbilityKind.Sleep] = [],
        [CopyAbilityKind.Plasma] = [typeof(PlasmaCannon), typeof(PlasmaCharge), typeof(UltimateFlash)],
        [CopyAbilityKind.Car] = [],
        [CopyAbilityKind.StarRod] = [],
        [CopyAbilityKind.Galaxia] = [],
        [CopyAbilityKind.Doctor] = [typeof(PillThrow), typeof(MakeMedicine), typeof(Experiment)],
        [CopyAbilityKind.Wheel] = [typeof(WheelDash), typeof(WheelCharge)],
    };

    /// <summary>The "gain copy ability X" card of each ability.</summary>
    private static readonly Dictionary<CopyAbilityKind, Type> AbilityCards = new()
    {
        [CopyAbilityKind.Sword] = typeof(SwordAbility),
        [CopyAbilityKind.Boomerang] = typeof(BoomerangAbility),
        [CopyAbilityKind.Stone] = typeof(StoneAbility),
        [CopyAbilityKind.Fighter] = typeof(FighterAbility),
        [CopyAbilityKind.Ice] = typeof(IceAbility),
        [CopyAbilityKind.Fire] = typeof(FireAbility),
        [CopyAbilityKind.Sleep] = typeof(SleepAbility),
        [CopyAbilityKind.Plasma] = typeof(PlasmaAbility),
        [CopyAbilityKind.Car] = typeof(CarKirby),
        [CopyAbilityKind.StarRod] = typeof(StarRodAbility),
        [CopyAbilityKind.Galaxia] = typeof(GalaxiaAbility),
        [CopyAbilityKind.Doctor] = typeof(DoctorAbility),
        [CopyAbilityKind.Wheel] = typeof(WheelieRider),
    };

    private static readonly Dictionary<CopyAbilityKind, Type> AbilityPowers = new()
    {
        [CopyAbilityKind.Sword] = typeof(SwordPower),
        [CopyAbilityKind.Boomerang] = typeof(BoomerangPower),
        [CopyAbilityKind.Stone] = typeof(StonePower),
        [CopyAbilityKind.Fighter] = typeof(FighterPower),
        [CopyAbilityKind.Ice] = typeof(IcePower),
        [CopyAbilityKind.Fire] = typeof(FirePower),
        [CopyAbilityKind.Sleep] = typeof(SleepPower),
        [CopyAbilityKind.Plasma] = typeof(PlasmaPower),
        [CopyAbilityKind.Car] = typeof(CarPower),
        [CopyAbilityKind.StarRod] = typeof(StarRodPower),
        [CopyAbilityKind.Galaxia] = typeof(GalaxiaPower),
        [CopyAbilityKind.Doctor] = typeof(DoctorPower),
        [CopyAbilityKind.Wheel] = typeof(WheelPower),
    };

    public const int GalaxiaRawDamage = 30;
    public const int StarRodHits = 5;

    private static readonly ConditionalWeakTable<CardModel, StrongBox<CopyAbilityKind>> GrantedCards = new();

    // Base-game moves that give Burn. A Burn actually stopped by Inhale also switches the ability to Fire.
    private static readonly HashSet<string> BurnMoves = ["FLAMETHROWER_MOVE", "BURNING_GROWL_MOVE"];

    /// <summary>Tags a card instance as handed out by <paramref name="kind" />, so losing that ability exhausts it.</summary>
    public static void MarkGranted(CardModel card, CopyAbilityKind kind) => GrantedCards.AddOrUpdate(card, new(kind));

    /// <summary>
    /// Whether <paramref name="card" /> belongs to a copy ability, and to which one (null kind: Drop Ability,
    /// exhausted once no ability is left).
    /// </summary>
    public static bool TryGetGrantingAbility(CardModel card, out CopyAbilityKind? kind)
    {
        if (card is ICopyAbilityCard abilityCard)
        {
            kind = abilityCard.GrantedBy;
            return true;
        }

        kind = GrantedCards.TryGetValue(card, out var box) ? box.Value : null;
        return kind != null;
    }

    /// <summary>
    /// Reads the enemy's current intents (DESIGN.md, 意图对应表). Mouthful prefers damage; the ability goes by
    /// priority: raw hit ≥ 30 (Galaxia) &gt; 5+ hits (Star Rod) &gt; Burn &gt; summon (Plasma) &gt; defend &gt; buff
    /// &gt; debuff &gt; attack &gt; Sleep.
    /// </summary>
    /// <summary>The ability and outcome for inhaling <paramref name="enemy" />; a null ability means a random one (unknown intent).</summary>
    public static (CopyAbilityKind? Kind, InhaleOutcome Outcome) ReadIntent(Creature enemy)
    {
        var move = enemy.Monster?.NextMove;
        var intents = move?.Intents ?? [];

        var attacks = intents.OfType<AttackIntent>().ToList();
        var attack = attacks.Count > 0;
        var multiAttack = attacks.Any(i => i is MultiAttackIntent);
        var maxHits = attack ? attacks.Max(i => i.Repeats) : 0;
        var maxRawDamage = attack ? attacks.Max(i => i.DamageCalc?.Invoke() ?? 0m) : 0m;
        var defend = intents.Any(i => i is DefendIntent);
        var buff = intents.Any(i => i is BuffIntent);
        var summon = intents.Any(i => i is SummonIntent);
        var status = intents.Any(i => i is StatusIntent);
        var debuff = intents.Any(i => i is DebuffIntent or CardDebuffIntent);
        var burn = status && move != null && BurnMoves.Contains(move.Id);
        var unknown = intents.Any(i => i is UnknownIntent or HiddenIntent);

        if (intents.Any(i => i is EscapeIntent))
            return (CopyAbilityKind.Wheel, InhaleOutcome.Escape);

        CopyAbilityKind? kind = maxRawDamage >= GalaxiaRawDamage ? CopyAbilityKind.Galaxia
            : maxHits >= StarRodHits ? CopyAbilityKind.StarRod
            : burn ? CopyAbilityKind.Fire
            : summon ? CopyAbilityKind.Plasma
            : defend ? CopyAbilityKind.Stone
            : buff ? CopyAbilityKind.Fighter
            : status ? CopyAbilityKind.Ice
            : debuff ? CopyAbilityKind.Doctor
            : attack ? multiAttack ? CopyAbilityKind.Boomerang : CopyAbilityKind.Sword
            : unknown ? null
            : CopyAbilityKind.Sleep;

        var outcome = attack ? InhaleOutcome.FirstHit
            : status ? InhaleOutcome.BlockFirstStatusCard
            : InhaleOutcome.Fallback;

        return (kind, outcome);
    }

    private sealed class RoundDamage
    {
        public int Round = -1;
        public int Total;
    }

    private static readonly ConditionalWeakTable<Player, RoundDamage> DamageThisRound = new();

    /// <summary>
    /// Adds damage Kirby took from enemies this round; true once the round's total reaches the threshold (the total
    /// then starts over, so a further threshold's worth triggers again).
    /// </summary>
    public static bool AddRoundDamage(Player player, int damage)
    {
        var round = player.Creature.CombatState?.RoundNumber ?? 0;
        var entry = DamageThisRound.GetOrCreateValue(player);
        if (entry.Round != round)
        {
            entry.Round = round;
            entry.Total = 0;
        }

        entry.Total += damage;
        if (entry.Total < HeavyHitThreshold(player))
            return false;

        entry.Total = 0;
        return true;
    }

    /// <summary>Damage within one round that makes Kirby lose his abilities (12 with Copy Essence Deluxe).</summary>
    public static int HeavyHitThreshold(Player player) =>
        player.GetRelic<CopyEssenceDeluxe>() != null ? CopyEssenceDeluxe.HeavyHit : 6;

    /// <summary>
    /// Enough damage this round: every held ability that can be lost this way is lost (its cards exhausted), or 1
    /// Ability Conservation is spent instead. The starter relic then returns Inhale.
    /// </summary>
    public static async Task OnHeavyHit(PlayerChoiceContext ctx, Player player)
    {
        var losing = Held(player).Where(power => power.LostToHeavyHits).ToList();
        if (losing.Count == 0)
            return;

        if (player.Creature.GetPower<AbilityConservationPower>() is { } conservation)
        {
            await PowerCmd.Decrement(conservation);
            return;
        }

        foreach (var power in losing)
            await KirbyMouth.LoseAbility(ctx, player, power);
        await AfterAbilityLost(player);
    }

    /// <summary>The starter relic's "an ability was lost" effect (returns Inhale), after damage or a deliberate drop.</summary>
    public static async Task AfterAbilityLost(Player player)
    {
        foreach (var relic in player.Relics.OfType<CopyAbilityRelicBase>().ToList())
            await relic.AfterAbilityLost();
    }

    /// <summary>Kirby's current copy abilities: one, or several with Smash Bros. Kirby.</summary>
    public static IReadOnlyList<CopyAbilityPower> Held(Player player) =>
        player.Creature.Powers.OfType<CopyAbilityPower>().ToList();

    public static bool Has(Player? player, IReadOnlyList<CopyAbilityKind> kinds) =>
        player != null && Held(player).Any(power => kinds.Contains(power.Kind));

    public static bool HasAny(Player player) => Held(player).Count > 0;

    /// <summary>
    /// Gives Kirby the copy ability <paramref name="kind" />. Without Smash Bros. Kirby the previous abilities are lost
    /// first (their cards exhausted); with it they stay, and regaining a held ability repeats its on-gain effect.
    /// Under Super Ability the same ability is gained twice: everything below happens a second time, as if it were
    /// regained while held.
    /// </summary>
    public static async Task Gain(PlayerChoiceContext ctx, Player player, CopyAbilityKind kind, bool upgraded,
        CardModel? source, PileType dropAbilityPile = PileType.Hand, bool keepOthers = false, bool allowEndTurn = true)
    {
        var kirby = player.Creature;

        var times = 1;
        if (kirby.GetPower<SuperAbilityPower>() is { } super)
        {
            times = 2;
            await PowerCmd.Decrement(super);
        }

        if (!keepOthers && !kirby.HasPower<SmashBrosPower>())
            await KirbyMouth.LoseCopyAbility(ctx, player);

        CopyAbilityPower? power = null;
        for (var i = 0; i < times; i++)
            power = await GainOnce(ctx, player, kind, upgraded, source, dropAbilityPile);

        // Last, and only once even when gained twice: Sleep ends the turn.
        if (allowEndTurn && power is { EndsTurnOnGain: true })
            PlayerCmd.EndTurn(player, canBackOut: false);
    }

    /// <summary>
    /// One gain of <paramref name="kind" />: apply the power (or reuse the held one), hand out its card and Drop
    /// Ability, fire the "whenever you gain a copy ability" effects, then its own on-gain effect.
    /// </summary>
    private static async Task<CopyAbilityPower> GainOnce(PlayerChoiceContext ctx, Player player, CopyAbilityKind kind,
        bool upgraded, CardModel? source, PileType dropAbilityPile)
    {
        var kirby = player.Creature;

        var power = Held(player).FirstOrDefault(held => held.Kind == kind);
        if (power == null)
        {
            power = (CopyAbilityPower)CanonicalPower(kind).ToMutable();
            power.Upgraded = upgraded;
            await PowerCmd.Apply(ctx, power, kirby, 1, kirby, source);
        }
        else
        {
            power.Upgraded |= upgraded;
        }

        await GrantAbilityCard(player, kind, upgraded);
        if (dropAbilityPile == PileType.Hand)
            await KirbyMouth.AddToHandIfMissing<DropAbility>(player);
        else if (!KirbyMouth.LivePiles.SelectMany(pile => pile.GetPile(player).Cards).Any(card => card is DropAbility))
            await CardPileCmd.AddGeneratedCardToCombat(KirbyMouth.CreateCard<DropAbility>(player, false),
                dropAbilityPile, player, CardPilePosition.Random);

        if (kirby.GetPower<GalaxyWishPower>() is { } wish)
            await PlayerCmd.GainEnergy(wish.Amount, player);
        if (kirby.HasPower<EnhancedInhalePower>())
            await KirbyMouth.PutInhaleIntoHand(player, includeLivePiles: true);

        await power.OnGained(ctx, player);
        return power;
    }

    /// <summary>
    /// Puts 1 random card of <paramref name="kind" /> into hand (upgraded if the ability is), Ethereal and Exhaust except
    /// a Burn. It is also tagged, so losing the ability exhausts it.
    /// </summary>
    private static async Task GrantAbilityCard(Player player, CopyAbilityKind kind, bool upgraded)
    {
        var candidates = AbilityCardTypes[kind];
        if (candidates.Length == 0)
            return;

        var type = player.RunState.Rng.CombatCardGeneration.NextItem(candidates.ToList())!;
        var card = await GrantCard(player, type, kind);
        if (card == null)
            return;

        if (upgraded && card.IsUpgradable)
            CardCmd.Upgrade(card);
        if (card is not Burn)
        {
            card.AddKeyword(CardKeyword.Ethereal);
            card.AddKeyword(CardKeyword.Exhaust);
        }
    }

    /// <summary>Creates a card of <paramref name="type" /> in hand, tagged as granted by <paramref name="kind" />.</summary>
    public static async Task<CardModel?> GrantCard(Player player, Type type, CopyAbilityKind kind)
    {
        var combatState = player.Creature.CombatState;
        if (combatState == null)
            return null;

        var card = combatState.CreateCard(ModelDb.GetById<CardModel>(ModelDb.GetId(type)), player);
        if (card is not Burn || kind != CopyAbilityKind.Fire)
            MarkGranted(card, kind);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
        return card;
    }

    /// <summary>The cards <paramref name="kind" /> hands out (Galaxia uses Sword's).</summary>
    public static IReadOnlyList<Type> CardsOf(CopyAbilityKind kind) => AbilityCardTypes[kind];

    public static CardModel CreateAbilityCard(Player player, CopyAbilityKind kind)
    {
        var combatState = player.Creature.CombatState
                          ?? throw new InvalidOperationException("Copy ability cards are only created in combat.");
        var card = combatState.CreateCard(ModelDb.GetById<CardModel>(ModelDb.GetId(AbilityCards[kind])), player);

        return card;
    }

    /// <summary>The canonical power of an ability (for hover tips; use ToMutable to apply it).</summary>
    public static PowerModel CanonicalPower(CopyAbilityKind kind) =>
        ModelDb.GetById<PowerModel>(ModelDb.GetId(AbilityPowers[kind]));
}
