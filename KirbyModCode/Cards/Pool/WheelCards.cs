using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace KirbyMod.Cards;

/// <summary>车轮驰骋: gain 4 Block 3(4) times. Wheel: Retain, and gain 1 Dexterity.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class WheelDash() : KirbyCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Wheel];

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(4, ValueProp.Move), new RepeatVar(3), new PowerVar<DexterityPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var synergy = SynergyActive;
        for (var i = 0; i < DynamicVars.Repeat.IntValue; i++)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (synergy)
            await PowerCmd.Apply<DexterityPower>(ctx, Owner.Creature, DynamicVars[nameof(DexterityPower)].BaseValue,
                Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1);
}

/// <summary>车轮蓄力: gain 1(2) Dexterity. Exhaust. Wheel: Retain, and gain 1 more Dexterity.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class WheelCharge() : KirbyCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Wheel];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<DexterityPower>(1), new DynamicVar("WheelDex", 1)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var dex = DynamicVars[nameof(DexterityPower)].BaseValue + (SynergyActive ? DynamicVars["WheelDex"].BaseValue : 0);
        await PowerCmd.Apply<DexterityPower>(ctx, Owner.Creature, dex, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars[nameof(DexterityPower)].UpgradeValueBy(1);
}

/// <summary>
/// 疯狂摩托手 (Wheelie Rider): the Wheel ability card, 2 cost, Exhaust. If you already have Wheel: deal Dexterity x 7(9)
/// damage to ALL enemies and gain 2 Dexterity. Then gain Wheel. Wheel: Retain.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class WheelieRider() : AbilityCard(CardRarity.Rare, 2)
{
    public override CopyAbilityKind Kind => CopyAbilityKind.Wheel;

    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Wheel];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("DexDamage", 7), new DynamicVar("WheelDex", 2)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [.. base.AdditionalHoverTips, HoverTipFactory.FromPower<DexterityPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        if (SynergyActive)
        {
            var dex = Owner.Creature.GetPowerAmount<DexterityPower>();
            if (dex > 0)
                await DamageCmd.Attack(dex * DynamicVars["DexDamage"].BaseValue).FromCard(this)
                    .TargetingAllOpponents(CombatState!).WithHitFx("vfx/vfx_heavy_blunt").Execute(ctx);
            await PowerCmd.Apply<DexterityPower>(ctx, Owner.Creature, DynamicVars["WheelDex"].BaseValue, Owner.Creature, this);
        }

        await base.OnPlay(ctx, cardPlay);
    }

    // Upgrade raises the damage only; the cost stays 2.
    protected override void OnUpgrade() => DynamicVars["DexDamage"].UpgradeValueBy(2);
}
