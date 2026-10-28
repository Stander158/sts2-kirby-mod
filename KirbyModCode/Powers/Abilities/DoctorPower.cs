using KirbyMod.Cards;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// 医生: at the start of each turn, apply 3 Poison to ALL enemies. Also carries Experiment's own "Doctor: costs 1 less"
/// (described on that card).
/// </summary>
[RegisterPower]
public sealed class DoctorPower : CopyAbilityPower
{
    public const int Poison = 3;

    public override CopyAbilityKind Kind => CopyAbilityKind.Doctor;

    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("regen_power");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Owner.CombatState == null)
            return;

        Flash();
        foreach (var enemy in Owner.CombatState.HittableEnemies.ToList())
        {
            VfxCmd.PlayOnCreatureCenter(enemy, "vfx/vfx_poison_impact");
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, Poison, Owner, null);
        }
    }

    // The game always uses modifiedCost (the return value is ignored), so other cards must get their cost back.
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        var applies = card is Experiment && card.Owner == Owner.Player;
        modifiedCost = applies ? Math.Max(0m, originalCost - 1) : originalCost;
        return applies;
    }
}
