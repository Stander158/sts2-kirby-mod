using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// 能力守恒: each stack blocks the Copy Ability relic once. Stacks do not wear off over time; the relic spends one
/// whenever it would have taken your ability (see <see cref="Relics.CopyAbility" />).
/// </summary>
[RegisterPower]
public sealed class AbilityConservationPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("barricade_power");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
