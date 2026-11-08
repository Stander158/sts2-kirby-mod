using Godot;
using KirbyMod.Cards;
using KirbyMod.Characters;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// 时钟计时 (enemies and players): at <see cref="Threshold" /> (plus that again per Timing Immunity) it is all removed
/// and the owner gains 1 Timing Immunity; an enemy is put in Stasis (a stun with its own intent), a player's turn ends.
/// </summary>
[RegisterPower]
public sealed class ClockPower : ModPowerTemplate
{
    public const int Threshold = 12;

    public override PowerAssetProfile AssetProfile => KirbyArt.Power("clock");

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<TimeImmunityPower>()];

    public override Task AfterApplied(Creature? applier, CardModel? cardSource) => Check(new BlockingPlayerChoiceContext());

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount,
        Creature? applier, CardModel? cardSource) =>
        power == this ? Check(choiceContext) : Task.CompletedTask;

    private async Task Check(PlayerChoiceContext ctx)
    {
        var needed = Threshold * (1 + Owner.GetPowerAmount<TimeImmunityPower>());
        if (Amount < needed || !Owner.IsAlive)
            return;

        await PowerCmd.Remove(this);
        await PowerCmd.Apply<TimeImmunityPower>(ctx, Owner, 1, Owner, null);

        if (Owner.Monster != null)
            Stasis.Apply(Owner);
        else if (Owner.Player is { } player && Owner.CombatState?.CurrentSide == CombatSide.Player)
            PlayerCmd.EndTurn(player, canBackOut: false);
    }
}

/// <summary>计时免疫: each stack raises the Clock needed to trigger by 12.</summary>
[RegisterPower]
public sealed class TimeImmunityPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyArt.Power("time_immunity");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}

/// <summary>究极光束: at the start of each of your turns, gain Amount Clock (for the rest of combat).</summary>
[RegisterPower]
public sealed class UltimateBeamPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("slow_power");

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<ClockPower>()];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        Flash();
        await PowerCmd.Apply<ClockPower>(choiceContext, Owner, Amount, Owner, null);
    }
}

/// <summary>停滞: the game's stun (skip the next move, then carry on), shown with its own intent.</summary>
public static class Stasis
{
    public const string IntentIcon = "res://KirbyMod/images/intents/stasis.png";

    /// <summary>Same as <c>Creature.StunInternal</c>, with <see cref="StasisIntent" /> instead of the stun intent.</summary>
    public static void Apply(Creature creature)
    {
        var monster = creature.Monster;
        if (monster == null || creature.CombatState == null || creature.IsDead)
            return;

        var nextMoveId = monster.MoveStateMachine.StateLog.Last().Id;
        var state = new MoveState("STUNNED", _ => ShowVfx(creature), new StasisIntent())
        {
            FollowUpStateId = nextMoveId,
            MustPerformOnceBeforeTransitioning = true,
        };
        monster.SetMoveImmediate(state);
    }

    private static Task ShowVfx(Creature creature)
    {
        var vfx = NStunnedVfx.Create(creature);
        var container = vfx == null ? null : creature.GetVfxContainer();
        if (container != null)
            Callable.From(() => container.AddChildSafely(vfx)).CallDeferred();
        return Task.CompletedTask;
    }
}

/// <summary>The Stasis intent: a stun (same <see cref="IntentType" />) with Kirby's name, text and icon.</summary>
public sealed class StasisIntent : StunIntent
{
    protected override string IntentPrefix => "KIRBY_MOD_STASIS";

    public override IEnumerable<string> AssetPaths => [];

    public override Texture2D? GetTexture(IEnumerable<Creature> targets, Creature owner) =>
        ResourceLoader.Load<Texture2D>(Stasis.IntentIcon);

    // The intent widget plays the stun animation.
    public override string GetAnimation(IEnumerable<Creature> targets, Creature owner) => "stun";
}
