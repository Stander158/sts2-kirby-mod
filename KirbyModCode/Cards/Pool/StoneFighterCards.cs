using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace KirbyMod.Cards;

/// <summary>无敌防御: gain 1(2) Buffer. Exhaust. Stone: Retain, also draw 1 card.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class InvincibleGuard() : KirbyCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Stone];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<BufferPower>(1), new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BufferPower>(ctx, Owner.Creature, DynamicVars[nameof(BufferPower)].BaseValue, Owner.Creature, this);
        if (SynergyActive)
            await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars[nameof(BufferPower)].UpgradeValueBy(1);
}

/// <summary>石头拳: gain 8(10) Block. Stone: Retain, and deal 15(21) damage per Buffer you have.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class StonePunch() : KirbyCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Stone];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0),
        new ExtraDamageVar(15),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(BufferStacks),
        new BlockVar(8, ValueProp.Move),
    ];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (SynergyActive)
            await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this, cardPlay).Targeting(cardPlay.Target!)
                .WithHitFx("vfx/vfx_heavy_blunt").Execute(ctx);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.ExtraDamage.UpgradeValueBy(6);
        DynamicVars.Block.UpgradeValueBy(2);
    }

    private static decimal BufferStacks(CardModel card, Creature? _) =>
        card.Owner.Creature.GetPower<BufferPower>()?.Amount ?? 0;
}

/// <summary>蓄力 (power): gain 1(2) Strength. Fighter: Retain, gain 1 more.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class ChargeUp() : KirbyCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    private const int FighterBonus = 1;

    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Fighter];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<StrengthPower>(1), new DynamicVar("FighterBonus", FighterBonus)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var strength = DynamicVars.Strength.BaseValue + (SynergyActive ? FighterBonus : 0);
        await PowerCmd.Apply<StrengthPower>(ctx, Owner.Creature, strength, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Strength.UpgradeValueBy(1);
}

/// <summary>真空打击: deal 1 damage 8(10) times. Fighter: Retain.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class VacuumStrike() : KirbyCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Fighter];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1, ValueProp.Move), new RepeatVar(8)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(DynamicVars.Repeat.IntValue).FromCard(this, cardPlay)
            .Targeting(cardPlay.Target!).WithHitFx("vfx/vfx_thrash").Execute(ctx);
    }

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(2);
}
