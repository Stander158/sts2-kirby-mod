using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>车轮: 6 Block when gained, +3 Dexterity while held (lost with the ability).</summary>
[RegisterPower]
public sealed class WheelPower : CopyAbilityPower
{
    public const int BlockOnGain = 6;
    public const int Dexterity = 3;

    public override CopyAbilityKind Kind => CopyAbilityKind.Wheel;

    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("speedster_power");

    public override async Task OnGained(PlayerChoiceContext ctx, Player player)
    {
        await CreatureCmd.GainBlock(Owner, BlockOnGain, ValueProp.Unpowered, null);
        await Grant<DexterityPower>(ctx, Dexterity);
    }
}
