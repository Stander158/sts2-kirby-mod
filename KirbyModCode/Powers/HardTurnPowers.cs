using KirbyMod.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Combat.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>急速转弯: Dexterity down until the end of the turn.</summary>
[RegisterPower]
public sealed class HardTurnDexterityPower : ModTemporaryAppliedPowerTemplate<HardTurn, DexterityPower>
{
    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("dexterity_power");

    protected override bool IsPositive => false;
}

/// <summary>急速转弯: Strength up until the end of the turn.</summary>
[RegisterPower]
public sealed class HardTurnStrengthPower : ModTemporaryAppliedPowerTemplate<HardTurn, StrengthPower>
{
    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("strength_power");

    protected override bool IsPositive => true;
}
