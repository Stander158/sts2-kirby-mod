using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>增强吸入: whenever you gain a copy ability, from any source, put Inhale into your hand.</summary>
[RegisterPower]
public sealed class EnhancedInhalePower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("juggernaut_power");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;
}
