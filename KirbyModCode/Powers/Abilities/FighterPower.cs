using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>格斗家: +3 Strength while held.</summary>
[RegisterPower]
public sealed class FighterPower : CopyAbilityPower
{
    public override CopyAbilityKind Kind => CopyAbilityKind.Fighter;

    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("strength_power");

    public override Task OnGained(PlayerChoiceContext ctx, Player player) =>
        Grant<StrengthPower>(ctx, 3);
}
