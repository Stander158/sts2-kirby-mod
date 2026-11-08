using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
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

/// <summary>光束攻击: deal 8(11); if the target intends to attack, apply 1(2) Weak. Beam: Retain, and 1 Clock to it.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class BeamAttack() : KirbyCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public const int Clock = 1;

    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Beam];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(8, ValueProp.Move), new PowerVar<WeakPower>(1), new DynamicVar("Clock", Clock)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<ClockPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        var attacking = target.Monster?.NextMove?.Intents.Any(intent => intent is AttackIntent) ?? false;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(ctx);
        if (!target.IsAlive)
            return;

        if (attacking)
            await PowerCmd.Apply<WeakPower>(ctx, target, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
        if (SynergyActive)
            await PowerCmd.Apply<ClockPower>(ctx, target, Clock, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars["WeakPower"].UpgradeValueBy(1);
    }
}

/// <summary>时间蓄力光束: deal 13(17) to ALL enemies. Beam: Retain, and 3 Clock to ALL enemies.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class TimeChargeBeam() : KirbyCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public const int Clock = 3;

    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Beam];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(13, ValueProp.Move), new DynamicVar("Clock", Clock)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<ClockPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(ctx);

        if (SynergyActive)
            await PowerCmd.Apply<ClockPower>(ctx, CombatState!.HittableEnemies.ToList(), Clock, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4);
}

/// <summary>
/// 究极光束 (Flare Beam): hit a random enemy for 12(14) twice, 1 Weak per hit; gain Beam; from now on gain 1 Clock at the
/// start of each turn. Beam (held when played): each hit also applies 2(4) Clock. Exhaust. No Retain.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class UltimateBeam() : KirbyCard(2, CardType.Attack, CardRarity.Rare, TargetType.RandomEnemy)
{
    private const int Hits = 2;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(12, ValueProp.Move), new PowerVar<WeakPower>(1), new DynamicVar("Clock", 2)];

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
                .FromCard(this)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(ctx);
            if (!target.IsAlive)
                continue;

            await PowerCmd.Apply<WeakPower>(ctx, target, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
            if (hadBeam)
                await PowerCmd.Apply<ClockPower>(ctx, target, DynamicVars["Clock"].BaseValue, Owner.Creature, this);
        }

        await CopyAbilities.Gain(ctx, Owner, CopyAbilityKind.Beam, false, this);
        await PowerCmd.Apply<UltimateBeamPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars["Clock"].UpgradeValueBy(2);
    }
}
