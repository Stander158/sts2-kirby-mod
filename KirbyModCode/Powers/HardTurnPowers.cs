using KirbyMod.Cards;
using MegaCrit.Sts2.Core.Localization;
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

    // The base game's temporary Dexterity text.
    public override LocString Description => new("powers", "TEMPORARY_DEXTERITY_DOWN.description");

    protected override string SmartDescriptionLocKey => "TEMPORARY_DEXTERITY_DOWN.smartDescription";
}
