using KirbyMod.Characters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>满嘴: a counter spent by Spit Star (damage) or Swallow. While Kirby has it, he is drawn with a full mouth.</summary>
[RegisterPower]
public sealed class MouthfulPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyArt.Power("star");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        KirbySprite.SetMouthful(Owner, true);
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        KirbySprite.SetMouthful(oldOwner, false);
        return Task.CompletedTask;
    }
}
