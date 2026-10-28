using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// 能力守恒: whenever an attack would make Kirby lose his copy abilities, 1 stack is spent instead (see
/// <see cref="Mechanics.CopyAbilities.OnHeavyHit" />). Stacks do not wear off over time.
/// </summary>
[RegisterPower]
public sealed class AbilityConservationPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("barricade_power");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
