using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace KirbyMod.Cards;

/// <summary>等离子炮: X cost, deal 9 damage X(+1) times. Plasma: Retain, and gain 1 energy afterwards.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class PlasmaCannon() : KirbyCard(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override bool HasEnergyCostX => true;

    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Plasma];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9, ValueProp.Move), new EnergyVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var synergy = SynergyActive;
        var hits = ResolveEnergyXValue() + (IsUpgraded ? 1 : 0);
        if (hits > 0)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(hits).FromCard(this, cardPlay)
                .Targeting(cardPlay.Target!).WithLightningHit().Execute(ctx);
        }

        if (synergy)
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }
}

/// <summary>等离子蓄力: gain 2 energy. Plasma: Retain, and gain 6 Block. Upgrade: cost 0.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class PlasmaCharge() : KirbyCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
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

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>
/// 究极闪光 (4 cost): deal 48(64) damage to ALL enemies; gain 3 energy if it killed an enemy (2026-11-18: no longer
/// needs Plasma). Plasma: Retain, and draw 2.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class UltimateFlash() : KirbyCard(4, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Plasma];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(48, ValueProp.Move), new EnergyVar(3), new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var synergy = SynergyActive;
        var enemies = CombatState!.HittableEnemies.ToList();
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay).TargetingAllOpponents(CombatState!)
            .WithLightningHit().Execute(ctx);

        if (enemies.Any(enemy => !enemy.IsAlive))
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        if (synergy)
            await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(16);
}
