using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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
        [.. base.AdditionalHoverTips, HoverTipFactory.FromPower<ClockPower>()];
}

/// <summary>光束攻击: deal 8(11); if the target intends to attack, apply 1(2) Weak. Beam: Retain, and 2(3) Clock to it.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class BeamAttack() : KirbyCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Beam];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(8, ValueProp.Move), new PowerVar<WeakPower>(1), new DynamicVar("Clock", 2)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<ClockPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        var attacking = target.Monster?.NextMove?.Intents.Any(intent => intent is AttackIntent) ?? false;

        KirbyFx.PlaySound(KirbyFx.BeamSound);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithBeam(Owner.Creature, () => [target])
            .Execute(ctx);
        if (!target.IsAlive)
            return;

        if (attacking)
            await PowerCmd.Apply<WeakPower>(ctx, target, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
        if (SynergyActive)
            await PowerCmd.Apply<ClockPower>(ctx, target, DynamicVars["Clock"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars["WeakPower"].UpgradeValueBy(1);
        DynamicVars["Clock"].UpgradeValueBy(1);
    }
}

/// <summary>时间蓄力光束: deal 13(17) to ALL enemies. Beam: Retain, and 3(4) Clock to ALL enemies.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class TimeChargeBeam() : KirbyCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Beam];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(13, ValueProp.Move), new DynamicVar("Clock", 3)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<ClockPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        KirbyFx.PlaySound(KirbyFx.TimeChargeBeamSound);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState!)
            .WithBeam(Owner.Creature, () => CombatState!.HittableEnemies.ToList(), impact: true)
            .Execute(ctx);

        if (SynergyActive)
            await PowerCmd.Apply<ClockPower>(ctx, CombatState!.HittableEnemies.ToList(), DynamicVars["Clock"].BaseValue,
                Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
        DynamicVars["Clock"].UpgradeValueBy(1);
    }
}

/// <summary>
/// 究极光束 (Flare Beam): 3 cost. Hit a random enemy for 12(14) three times, 1 Weak per hit; from now on gain 1 Clock at
/// the start of each turn. Beam (held when played): each hit also applies 3(4) Clock; otherwise gain Beam. Exhaust. No Retain.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class UltimateBeam() : KirbyCard(3, CardType.Attack, CardRarity.Rare, TargetType.RandomEnemy)
{
    private const int Hits = 3;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(12, ValueProp.Move), new PowerVar<WeakPower>(1), new DynamicVar("Clock", 3)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<ClockPower>(),
        HoverTipFactory.FromPower(CopyAbilities.CanonicalPower(CopyAbilityKind.Beam)),
    ];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var hadBeam = CopyAbilities.Has(Owner, [CopyAbilityKind.Beam]);

        for (var i = 0; i < Hits; i++)
        {
            var enemies = CombatState!.HittableEnemies.ToList();
            if (enemies.Count == 0)
                break;

            var target = Owner.RunState.Rng.CombatTargets.NextItem(enemies)!;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(target)
                .WithBeam(Owner.Creature, () => [target], impact: true)
                .Execute(ctx);
            if (!target.IsAlive)
                continue;

            await PowerCmd.Apply<WeakPower>(ctx, target, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
            if (hadBeam)
                await PowerCmd.Apply<ClockPower>(ctx, target, DynamicVars["Clock"].BaseValue, Owner.Creature, this);
        }

        if (!hadBeam)
            await CopyAbilities.Gain(ctx, Owner, CopyAbilityKind.Beam, false, this);
        await PowerCmd.Apply<UltimateBeamPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars["Clock"].UpgradeValueBy(1);
    }
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
