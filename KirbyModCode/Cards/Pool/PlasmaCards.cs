using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace KirbyMod.Cards;

/// <summary>等离子炮: X cost, deal 7 damage X(+1) times. Plasma: Retain, and gain 1 energy afterwards.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class PlasmaCannon() : KirbyCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override bool HasEnergyCostX => true;

    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Plasma];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7, ValueProp.Move), new EnergyVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var synergy = SynergyActive;
        var hits = ResolveEnergyXValue() + (IsUpgraded ? 1 : 0);
        if (hits > 0)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(hits).FromCard(this)
                .Targeting(cardPlay.Target!).WithHitFx("vfx/vfx_attack_blunt").Execute(ctx);
        }

        if (synergy)
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }
}

/// <summary>等离子蓄力: gain 2 energy. Plasma: Retain, and gain 6(9) Block. Upgrade: cost 0.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class PlasmaCharge() : KirbyCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Plasma];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2), new BlockVar(6, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var synergy = SynergyActive;
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        if (synergy)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars.Block.UpgradeValueBy(3);
    }
}

/// <summary>究极闪光: deal 28(40) damage to ALL enemies. Plasma: Retain, and gain 6(9) Block.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class UltimateFlash() : KirbyCard(3, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Plasma];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(28, ValueProp.Move), new BlockVar(6, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var synergy = SynergyActive;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState!)
            .WithHitFx("vfx/vfx_attack_blunt").Execute(ctx);
        if (synergy)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(12);
        DynamicVars.Block.UpgradeValueBy(3);
    }
}
