using KirbyMod.Characters;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// 向银河许愿: whenever you gain a copy ability, gain 1 energy per stack.
/// </summary>
[RegisterPower]
public sealed class GalaxyWishPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyArt.Power("galactic_nova");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}

/// <summary>向银河许愿+ (upgraded Milky Way Wishes): copy ability cards are no longer exhausted (see <see cref="Cards.AbilityCard" />).</summary>
[RegisterPower]
public sealed class GalaxyWishRecyclePower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyArt.Power("galactic_nova");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;
}
