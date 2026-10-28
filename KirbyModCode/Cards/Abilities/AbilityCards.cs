using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;

namespace KirbyMod.Cards;

/// <summary>剑能力: Uncommon.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class SwordAbility() : AbilityCard(CardRarity.Uncommon)
{
    public override CopyAbilityKind Kind => CopyAbilityKind.Sword;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Vigor", SwordPower.VigorOnGain), new DynamicVar("SwordStrength", SwordPower.Strength)];
}

/// <summary>回旋镖能力: Uncommon.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class BoomerangAbility() : AbilityCard(CardRarity.Uncommon)
{
    public override CopyAbilityKind Kind => CopyAbilityKind.Boomerang;
}

/// <summary>石头能力: Uncommon, still a 2(1)-cost power.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class StoneAbility() : AbilityCard(CardRarity.Uncommon, 2, CardType.Power)
{
    public override CopyAbilityKind Kind => CopyAbilityKind.Stone;
}

/// <summary>格斗家能力: Uncommon.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class FighterAbility() : AbilityCard(CardRarity.Uncommon)
{
    public override CopyAbilityKind Kind => CopyAbilityKind.Fighter;
}

/// <summary>冰能力: Rare. Upgrade also makes Ice Blocks upgraded (14 damage / 11 Block).</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class IceAbility() : AbilityCard(CardRarity.Rare)
{
    public override CopyAbilityKind Kind => CopyAbilityKind.Ice;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("IceBlock", IceBlock.BaseBlock), new DynamicVar("IceDamage", IceBlock.BaseDamage)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [.. base.AdditionalHoverTips, HoverTipFactory.FromCard<IceBlock>(IsUpgraded)];

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["IceBlock"].UpgradeValueBy(IceBlock.BlockUpgrade);
        DynamicVars["IceDamage"].UpgradeValueBy(IceBlock.DamageUpgrade);
    }
}

/// <summary>火能力: Rare. Burns can be played for free while Fire is held.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class FireAbility() : AbilityCard(CardRarity.Rare)
{
    public override CopyAbilityKind Kind => CopyAbilityKind.Fire;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Burns", FirePower.BurnsOnGain), new DynamicVar("BurnDamage", FirePower.BurnDamage)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [.. base.AdditionalHoverTips, HoverTipFactory.FromCard<Burn>()];

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["BurnDamage"].UpgradeValueBy(FirePower.UpgradedBurnDamageBonus);
    }
}

/// <summary>睡眠能力: Rare. Upgrade: 3 Strength / 3 Dexterity and heal 12 on waking.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class SleepAbility() : AbilityCard(CardRarity.Rare)
{
    public override CopyAbilityKind Kind => CopyAbilityKind.Sleep;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("SleepStats", SleepPower.Stats), new DynamicVar("SleepHeal", SleepPower.Heal)];

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["SleepStats"].UpgradeValueBy(1);
        DynamicVars["SleepHeal"].UpgradeValueBy(SleepPower.UpgradedHeal - SleepPower.Heal);
    }
}

/// <summary>等离子能力: Uncommon.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class PlasmaAbility() : AbilityCard(CardRarity.Uncommon)
{
    public override CopyAbilityKind Kind => CopyAbilityKind.Plasma;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(PlasmaPower.Energy)];
}

/// <summary>汽车卡比: Common. On gain deal 13(18) damage to ALL enemies; 6 more at the start of each turn.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class CarKirby() : AbilityCard(CardRarity.Common)
{
    public override CopyAbilityKind Kind => CopyAbilityKind.Car;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(CarPower.DamageOnGain, MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered),
         new DynamicVar("TurnDamage", CarPower.DamagePerTurn)];

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars.Damage.UpgradeValueBy(CarPower.UpgradeBonus);
    }
}

/// <summary>星之杖: Rare, 2(1) cost.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class StarRodAbility() : AbilityCard(CardRarity.Rare, 2)
{
    public override CopyAbilityKind Kind => CopyAbilityKind.StarRod;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Mouthful", StarRodPower.Mouthful), new DynamicVar("MouthfulPerTurn", StarRodPower.MouthfulPerTurn)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [.. base.AdditionalHoverTips, HoverTipFactory.FromCard<SpitStar>(true)];
}

/// <summary>万能剑 (Galaxia): Rare, 2(1) cost.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class GalaxiaAbility() : AbilityCard(CardRarity.Rare, 2)
{
    public override CopyAbilityKind Kind => CopyAbilityKind.Galaxia;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("GalaxiaStrength", GalaxiaPower.Strength), new CardsVar(GalaxiaPower.Cards)];
}

/// <summary>医生能力: Uncommon.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class DoctorAbility() : AbilityCard(CardRarity.Uncommon)
{
    public override CopyAbilityKind Kind => CopyAbilityKind.Doctor;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DoctorPoison", DoctorPower.Poison)];
}
