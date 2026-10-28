using KirbyMod.Characters;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>满嘴: a counter spent by Spit Star (damage) or Swallow (healing).</summary>
[RegisterPower]
public sealed class MouthfulPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyArt.Power("star");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
