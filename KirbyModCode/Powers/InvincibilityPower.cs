using KirbyMod.Characters;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>无敌: take no damage. Loses 1 stack at the end of each enemy turn (like Intangible, but 0 instead of 1).</summary>
[RegisterPower]
public sealed class InvincibilityPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyArt.Power("invincibility_candy");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource) =>
        CombatManager.Instance.IsInProgress && target == Owner ? 0m : amount;

    public override Task AfterModifyingHpLostAfterOsty()
    {
        Flash();
        return Task.CompletedTask;
    }

    // Same cap for block loss and damage previews.
    public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource) =>
        target == Owner ? 0m : decimal.MaxValue;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy)
            await PowerCmd.Decrement(this);
    }
}
