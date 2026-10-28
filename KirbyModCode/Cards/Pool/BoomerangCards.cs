using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace KirbyMod.Cards;

/// <summary>回旋镖: deal 3(4) damage to ALL enemies twice, gain 2(3) Block twice. Boomerang: Retain, +3(5) per hit.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class BoomerangThrow() : KirbyCard(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Boomerang];

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [.. SynergyDamage(3, 3), new RepeatVar(2), new BlockVar(2, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).WithHitCount(DynamicVars.Repeat.IntValue).FromCard(this)
            .TargetingAllOpponents(CombatState!).WithHitFx("vfx/vfx_flying_slash").Execute(ctx);
        for (var i = 0; i < DynamicVars.Repeat.IntValue; i++)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(1);
        DynamicVars.ExtraDamage.UpgradeValueBy(2);
        DynamicVars.Block.UpgradeValueBy(1);
    }
}

/// <summary>终极切割: deal 2(3) damage to ALL enemies 4 times, gain 6 Block. Boomerang: Retain, +2(3) per hit.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class FinalCutter() : KirbyCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Boomerang];

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [.. SynergyDamage(2, 2), new RepeatVar(4), new BlockVar(6, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).WithHitCount(DynamicVars.Repeat.IntValue).FromCard(this)
            .TargetingAllOpponents(CombatState!).WithHitFx("vfx/vfx_big_slash").Execute(ctx);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(1);
        DynamicVars.ExtraDamage.UpgradeValueBy(1);
    }
}
