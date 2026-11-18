using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace KirbyMod.Cards;

/// <summary>光束能力: Uncommon.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class BeamAbility() : AbilityCard(CardRarity.Uncommon)
{
    public override CopyAbilityKind Kind => CopyAbilityKind.Beam;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [.. base.AdditionalHoverTips, .. BeamClock.ClockTips()];
}

/// <summary>
/// 光束攻击: deal 8(11) and apply Clock equal to the turn number; if the target intends to attack, apply 1(2) Weak.
/// Beam: Retain.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class BeamAttack() : KirbyCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Beam];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(8, ValueProp.Move), new PowerVar<WeakPower>(1), .. BeamClock.TurnClock(1)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>(), .. BeamClock.ClockTips()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        var attacking = target.Monster?.NextMove?.Intents.Any(intent => intent is AttackIntent) ?? false;
        var clock = BeamClock.Amount(this);

        KirbyFx.PlaySound(KirbyFx.BeamSound);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithBeam(Owner.Creature, () => [target])
            .Execute(ctx);
        if (!target.IsAlive)
            return;

        await PowerCmd.Apply<ClockPower>(ctx, target, clock, Owner.Creature, this);
        if (attacking && target.IsAlive)
            await PowerCmd.Apply<WeakPower>(ctx, target, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars["WeakPower"].UpgradeValueBy(1);
    }
}

/// <summary>
/// 时间蓄力光束: deal 11(14) to ALL enemies and apply Clock equal to the turn number to ALL enemies. Every turn it stays
/// in the hand it deals 4 more and applies 2 more Clock for the rest of combat. Beam: Retain.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class TimeChargeBeam() : KirbyCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Beam];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(11, ValueProp.Move), new DynamicVar("Charge", 4), new DynamicVar("ClockCharge", 2),
        .. BeamClock.TurnClock(1),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => BeamClock.ClockTips();

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var clock = BeamClock.Amount(this);

        KirbyFx.PlaySound(KirbyFx.TimeChargeBeamSound);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState!)
            .WithBeam(Owner.Creature, () => CombatState!.HittableEnemies.ToList(), impact: true)
            .Execute(ctx);

        var enemies = CombatState!.HittableEnemies.ToList();
        if (enemies.Count > 0)
            await PowerCmd.Apply<ClockPower>(ctx, enemies, clock, Owner.Creature, this);
    }

    // Runs after the hand is flushed, so only a card that was kept (Retain) is still in the hand. The Clock bonus goes
    // on CalculationBase ({Clock} = CalculationBase + turn number).
    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || Pile?.Type != PileType.Hand || !participants.Contains(Owner.Creature))
            return Task.CompletedTask;

        DynamicVars.Damage.BaseValue += DynamicVars["Charge"].BaseValue;
        DynamicVars.CalculationBase.BaseValue += DynamicVars["ClockCharge"].BaseValue;
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

/// <summary>
/// 究极光束 (Flare Beam): 2 cost. Deal 13(17) and apply 12 Clock. If that puts the target into Stasis, it goes back to
/// the hand instead of being exhausted. Exhaust. Beam (held when played): Retain, and causing Stasis also gives an
/// extra turn. Otherwise, gain Beam. The extra turn uses the game's own mechanic the way Pael's Eye does: the card
/// answers ShouldTakeExtraTurn itself (cards in every pile, Exhaust included, get combat hooks), so no status is shown.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class UltimateBeam() : KirbyCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Beam];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(13, ValueProp.Move), new DynamicVar("Clock", 12)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        .. BeamClock.ClockTips(), HoverTipFactory.FromPower<StasisPower>(),
        HoverTipFactory.FromPower(CopyAbilities.CanonicalPower(CopyAbilityKind.Beam)),
    ];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        var hadBeam = CopyAbilities.Has(Owner, [CopyAbilityKind.Beam]);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithBeam(Owner.Creature, () => [target], impact: true)
            .Execute(ctx);

        var stasis = false;
        if (target.IsAlive)
        {
            var wasInStasis = target.GetPower<ClockPower>()?.InStasis ?? false;
            await PowerCmd.Apply<ClockPower>(ctx, target, DynamicVars["Clock"].BaseValue, Owner.Creature, this);
            stasis = !wasInStasis && (target.GetPower<ClockPower>()?.InStasis ?? false);
        }

        if (stasis && hadBeam)
            ExtraTurns++;
        if (!hadBeam)
            await CopyAbilities.Gain(ctx, Owner, CopyAbilityKind.Beam, false, this);

        // Moving it out of the Play pile now means the card play no longer exhausts it.
        if (stasis && Pile?.Type == PileType.Play)
            await CardPileCmd.Add(this, PileType.Hand);
    }

    private int _extraTurns;

    private int _extraTurnPaidOn;

    /// <summary>Extra turns this card still owes its owner.</summary>
    private int ExtraTurns
    {
        get => _extraTurns;
        set
        {
            AssertMutable();
            _extraTurns = value;
        }
    }

    public override bool ShouldTakeExtraTurn(Player player) => player == Owner && ExtraTurns > 0;

    // Every Ultimate Beam that owes a turn says yes, but only one of them pays for each extra turn taken.
    public override Task AfterTakingExtraTurn(Player player)
    {
        if (player != Owner || ExtraTurns <= 0 || Owner.PlayerCombatState is not { } state)
            return Task.CompletedTask;

        var turn = state.TurnNumber;
        if (state.AllCards.OfType<UltimateBeam>().Any(card => card._extraTurnPaidOn == turn))
            return Task.CompletedTask;

        AssertMutable();
        _extraTurnPaidOn = turn;
        ExtraTurns--;
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4);
}

/// <summary>
/// "Clock equal to the turn number": {Clock} = CalculationBase (0) + CalculationExtra x the combat's round number.
/// </summary>
internal static class BeamClock
{
    public static IEnumerable<DynamicVar> TurnClock(decimal perTurn) =>
        [new CalculationBaseVar(0), new CalculationExtraVar(perTurn), new CalculatedVar("Clock").WithMultiplier(Round)];

    public static decimal Round(CardModel card, Creature? _) =>
        card.IsMutable ? card.CombatState?.RoundNumber ?? 0 : 0;

    public static decimal Amount(CardModel card) => ((CalculatedVar)card.DynamicVars["Clock"]).Calculate(null);

    /// <summary>Clock's tip plus Timing Immunity's (a card's power tip does not bring the power's own tips along).</summary>
    public static IEnumerable<IHoverTip> ClockTips() =>
        [HoverTipFactory.FromPower<ClockPower>(), HoverTipFactory.FromPower<TimeImmunityPower>()];
}

/// <summary>Beam hit effects: Defect's Sweeping Beam from Kirby to the targets (it has no sound of its own).</summary>
internal static class BeamFx
{
    /// <param name="impact">Also burst Hyperbeam's impact on each target (the big beams).</param>
    public static AttackCommand WithBeam(this AttackCommand attack, Creature owner, Func<List<Creature>> targets,
        bool impact = false) =>
        attack.WithHitFx("vfx/vfx_attack_lightning").BeforeDamage(async () =>
        {
            var hit = targets();
            var container = NCombatRoom.Instance?.CombatVfxContainer;
            if (container == null || hit.Count == 0)
                return;

            var beam = NSweepingBeamVfx.Create(owner, hit);
            if (beam == null)
                return;

            container.AddChildSafely(beam);
            await Cmd.Wait(0.4f);
            if (!impact)
                return;

            foreach (var target in hit)
                if (NHyperbeamImpactVfx.Create(owner, target) is { } burst)
                    container.AddChildSafely(burst);
        });
}
