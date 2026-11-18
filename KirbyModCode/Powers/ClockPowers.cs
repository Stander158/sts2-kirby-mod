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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// 时钟计时 (enemies and players). Clock points read it like a clock face: 0 stacks = 0, otherwise 1–12 going round
/// (12 = 12, 13 = 1). Every 2 stacks (not points, so it keeps growing past 12 with Timing Immunity: 14 stacks = +70%)
/// make the owner take 10% more attack damage. At <see cref="Threshold" /> (plus
/// that again per Timing Immunity) an enemy goes into Stasis (a stun with its own intent) and every living player
/// draws 1 and gains 1 energy. While in Stasis it shows 12 points and gains no Clock; when the stunned move is spent
/// it is removed and the enemy gains 1 Timing Immunity (<see cref="EndStasis" />). A player at the threshold loses it
/// at once, gains Timing Immunity and gets <see cref="StasisPower" /> (no card gives players Clock any more).
/// </summary>
[RegisterPower]
public sealed class ClockPower : ModPowerTemplate
{
    public const int Threshold = 12;
    public const int MaxPoints = 12;
    public const int StacksPerSlow = 2;
    public const int SlowPercent = 10;
    public const int StasisEnergy = 1;
    public const int StasisDraw = 1;

    private sealed class Data
    {
        public bool InStasis;
    }

    public override PowerAssetProfile AssetProfile => KirbyArt.Power("clock");

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    // The tip shows the stacks needed right now, the clock points and the extra attack damage they give.
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Threshold", Threshold), new DynamicVar("ClockPoints", 0), new DynamicVar("SlowPercent", 0),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<StasisPower>(), HoverTipFactory.FromPower<TimeImmunityPower>()];

    private Data State => GetInternalData<Data>();

    protected override object InitInternalData() => new Data();

    public bool InStasis => State.InStasis;

    /// <summary>0 at 0 stacks, otherwise 1–12 going round; 12 while in Stasis.</summary>
    public int Points => State.InStasis ? MaxPoints : Amount <= 0 ? 0 : (Amount - 1) % MaxPoints + 1;

    /// <summary>The Clock points <paramref name="creature" /> has (0 without Clock).</summary>
    public static int PointsOf(Creature creature) => creature.GetPower<ClockPower>()?.Points ?? 0;

    private int SlowStacks => Math.Max(0, Amount) / StacksPerSlow;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource) => Check(new BlockingPlayerChoiceContext());

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount,
        Creature? applier, CardModel? cardSource)
    {
        if (power == this)
            return Check(choiceContext);
        if (power is TimeImmunityPower && power.Owner == Owner)
            UpdateVars();
        return Task.CompletedTask;
    }

    // Stuck in Stasis: no more Clock until it ends.
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount,
        Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (canonicalPower is not ClockPower || target != Owner || !State.InStasis || amount <= 0)
            return false;

        modifiedAmount = 0;
        return true;
    }

#if STS2_V113
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
#else
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource) =>
#endif
        target == Owner && props.IsPoweredAttack() ? 1m + SlowStacks * SlowPercent / 100m : 1m;

    private int UpdateVars()
    {
        var needed = Threshold * (1 + Owner.GetPowerAmount<TimeImmunityPower>());
        DynamicVars["Threshold"].BaseValue = needed;
        DynamicVars["ClockPoints"].BaseValue = Points;
        DynamicVars["SlowPercent"].BaseValue = SlowStacks * SlowPercent;
        return needed;
    }

    private async Task Check(PlayerChoiceContext ctx)
    {
        var needed = UpdateVars();
        if (State.InStasis || Amount < needed || !Owner.IsAlive)
            return;

        if (Owner.Monster != null)
        {
            if (!Stasis.Apply(Owner))
                return;

            State.InStasis = true;
            UpdateVars();
            Flash();
            foreach (var player in Owner.CombatState!.Players.Where(player => player.Creature.IsAlive).ToList())
            {
                await PlayerCmd.GainEnergy(StasisEnergy, player);
                await CardPileCmd.Draw(ctx, StasisDraw, player);
            }

            return;
        }

        await PowerCmd.Remove(this);
        await PowerCmd.Apply<TimeImmunityPower>(ctx, Owner, 1, Owner, null);
        if (Owner.Player != null)
            await PowerCmd.Apply<StasisPower>(ctx, Owner, 1, Owner, null);
    }

    /// <summary>The stunned move is spent: Clock is removed and the enemy gains 1 Timing Immunity.</summary>
    public static async Task EndStasis(Creature creature)
    {
        if (!creature.IsAlive || creature.GetPower<ClockPower>() is not { InStasis: true } clock)
            return;

        await PowerCmd.Remove(clock);
        await PowerCmd.Apply<TimeImmunityPower>(new ThrowingPlayerChoiceContext(), creature, 1, creature, null);
    }
}

/// <summary>
/// 停滞 on a player (from Clock): none of their cards can be played until the end of their turn, like the Ringing roar
/// of the Ceremonial Beast. Gained on the enemy turn, it lasts through the next player turn.
/// </summary>
[RegisterPower]
public sealed class StasisPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyArt.Power("stasis");

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) => card.Owner.Creature != Owner;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
            await PowerCmd.Remove(this);
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

/// <summary>停滞: the game's stun (skip the next move, then carry on), shown with its own intent.</summary>
public static class Stasis
{
    public const string IntentIcon = "res://KirbyMod/images/intents/stasis.png";

    /// <summary>
    /// Same as <c>Creature.StunInternal</c>, with <see cref="StasisIntent" /> instead of the stun intent. Spending the
    /// stunned move ends the Stasis (<see cref="ClockPower.EndStasis" />). False when it could not be stunned.
    /// </summary>
    public static bool Apply(Creature creature)
    {
        var monster = creature.Monster;
        if (monster == null || creature.CombatState == null || creature.IsDead)
            return false;

        var nextMoveId = monster.MoveStateMachine.StateLog.Last().Id;
        var state = new MoveState("STUNNED", async _ =>
        {
            await ShowVfx(creature);
            await ClockPower.EndStasis(creature);
        }, new StasisIntent())
        {
            FollowUpStateId = nextMoveId,
            MustPerformOnceBeforeTransitioning = true,
        };
        monster.SetMoveImmediate(state);
        return true;
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
