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
                .Targeting(cardPlay.Target!).WithHitFx("vfx/vfx_attack_lightning").Execute(ctx);
        }

        if (synergy)
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }
}

/// <summary>等离子蓄力: gain 2 energy. Plasma: Retain, and gain 6(7) Block. Upgrade: cost 0.</summary>
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

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars.Block.UpgradeValueBy(1);
    }
}

/// <summary>
/// 究极闪光: deal 15(19) damage to ALL enemies, +3(4) for each other card played this turn. Plasma: Retain, and +9(11)
/// damage per energy left after paying for it.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class UltimateFlash() : KirbyCard(3, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Plasma];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(15),
        new ExtraDamageVar(3),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(OtherCardsPlayedThisTurn),
        new DynamicVar("EnergyDamage", 9),
    ];

    private static decimal OtherCardsPlayedThisTurn(CardModel card, Creature? _) =>
        card.IsMutable && card.CombatState != null
            ? CombatManager.Instance.History.CardPlaysStarted.Count(entry =>
                entry.HappenedThisTurn(card.CombatState) && entry.CardPlay.Card.Owner == card.Owner && entry.CardPlay.Card != card)
            : 0;

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var damage = DynamicVars.CalculationBase.BaseValue
                     + DynamicVars.ExtraDamage.BaseValue * OtherCardsPlayedThisTurn(this, null);
        if (SynergyActive)
            damage += DynamicVars["EnergyDamage"].BaseValue * (Owner.PlayerCombatState?.Energy ?? 0);

        await DamageCmd.Attack(damage).FromCard(this).TargetingAllOpponents(CombatState!)
            .WithHitFx("vfx/vfx_attack_lightning").Execute(ctx);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(4);
        DynamicVars.ExtraDamage.UpgradeValueBy(1);
        DynamicVars["EnergyDamage"].UpgradeValueBy(2);
    }
}
