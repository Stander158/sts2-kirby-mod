using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>回旋镖: +2 Strength and +1 Dexterity while held.</summary>
[RegisterPower]
public sealed class BoomerangPower : CopyAbilityPower
{
    public override CopyAbilityKind Kind => CopyAbilityKind.Boomerang;

    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("dexterity_power");

    public override async Task OnGained(PlayerChoiceContext ctx, Player player)
    {
        await Grant<StrengthPower>(ctx, 2);
        await Grant<DexterityPower>(ctx, 1);
    }
}
