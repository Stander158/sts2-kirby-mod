using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>石头: 1 Buffer on gain. Losing the ability takes back only 1 Buffer, even if it was gained twice.</summary>
[RegisterPower]
public sealed class StonePower : CopyAbilityPower
{
    public override CopyAbilityKind Kind => CopyAbilityKind.Stone;

    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("buffer_power");

    public override Task OnGained(PlayerChoiceContext ctx, Player player) =>
        Grant<BufferPower>(ctx, 1);

    public override Task OnLost(PlayerChoiceContext ctx, Player player)
    {
        if (State.Granted.ContainsKey(typeof(BufferPower)))
            State.Granted[typeof(BufferPower)] = 1;
        return base.OnLost(ctx, player);
    }
}
