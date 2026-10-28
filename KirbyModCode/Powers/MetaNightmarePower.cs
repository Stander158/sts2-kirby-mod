using KirbyMod.Cards;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>魅塔梦魇: this turn, your Sword cards cost 0 (including ones drawn or made later this turn).</summary>
[RegisterPower]
public sealed class MetaNightmarePower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("sword_sage_power");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>Sword cards: cards that get a Sword bonus (Mach Tornado included), plus Meta Knight's Revenge.</summary>
    public static bool IsSwordCard(CardModel card) =>
        card is MetaKnightRevenge
        || (card is IAbilitySynergyCard synergy && synergy.SynergyAbilities.Contains(CopyAbilityKind.Sword));

    // The game always uses modifiedCost (the return value is ignored), so other cards must get their cost back.
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        var applies = card.Owner == Owner.Player && IsSwordCard(card);
        modifiedCost = applies ? 0m : originalCost;
        return applies;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
            await PowerCmd.Remove(this);
    }
}
