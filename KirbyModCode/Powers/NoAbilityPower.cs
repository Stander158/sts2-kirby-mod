using KirbyMod.Characters;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// 无能力 (No Ability), on Waddle Dees: inhaling this enemy never puts an ability in Kirby's mouth, whatever its intent
/// (KirbyMouth.StartInhale), swallowed whole or not.
/// </summary>
[RegisterPower]
public sealed class NoAbilityPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyArt.Power("normal_ability");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;
}
