using KirbyMod.Characters;
using KirbyMod.Cards;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// 剑: +2 Strength while held and 5 Vigor on gain (unused Vigor is lost with the ability). Also carries Meta Knight's Revenge's own "Sword: costs 0"
/// (described on that card, not here).
/// </summary>
[RegisterPower]
public sealed class SwordPower : CopyAbilityPower
{
    public const int VigorOnGain = 5;
    public const int Strength = 2;

    public override CopyAbilityKind Kind => CopyAbilityKind.Sword;

    public override PowerAssetProfile AssetProfile => KirbyArt.Power("sword_ability");

    public override async Task OnGained(PlayerChoiceContext ctx, Player player)
    {
        await Grant<StrengthPower>(ctx, Strength);
        await Grant<VigorPower>(ctx, VigorOnGain);
    }

    // The game always uses modifiedCost (the return value is ignored), so other cards must get their cost back.
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        var applies = card is MetaKnightRevenge && card.Owner == Owner.Player;
        modifiedCost = applies ? 0m : originalCost;
        return applies;
    }
}
