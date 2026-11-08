using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>光束: at the start of each turn, apply 1 Clock to ALL enemies. V-O-I-D counts as Beam but does not do this.</summary>
[RegisterPower]
public sealed class BeamPower : CopyAbilityPower
{
    public const int ClockPerTurn = 1;

    public override CopyAbilityKind Kind => CopyAbilityKind.Beam;

    public override PowerAssetProfile AssetProfile => KirbyArt.Power("clock");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Owner.CombatState == null)
            return;

        Flash();
        foreach (var enemy in Owner.CombatState.HittableEnemies.ToList())
            await PowerCmd.Apply<ClockPower>(choiceContext, enemy, ClockPerTurn, Owner, null);
    }
}
