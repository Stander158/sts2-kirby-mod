using KirbyMod.Characters;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// 究极之果: for the rest of combat, the inhaled enemy's first hit on Kirby deals 0 (it still becomes Mouthful).
/// The effect itself lives in <see cref="InhalingPower" />.
/// </summary>
[RegisterPower]
public sealed class UltimateFruitPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("regen_power");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;
}
