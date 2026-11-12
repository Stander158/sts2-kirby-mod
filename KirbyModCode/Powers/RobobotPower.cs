using KirbyMod.Characters;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>萝卜机甲 (does not stack): Strikes deal 3 more damage, Defends give 3 more Block. Inhale becomes Scan.</summary>
[RegisterPower]
public sealed class RobobotPower : ModPowerTemplate
{
    public const int Bonus = 3;

    public override PowerAssetProfile AssetProfile => KirbyArt.Power("robobot_armor");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

#if STS2_V113
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
#else
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource) =>
#endif
        IsOwnCard(cardSource, CardTag.Strike) && props.IsPoweredAttack() ? Bonus : 0m;

    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource,
        CardPlay? cardPlay) =>
        IsOwnCard(cardSource, CardTag.Defend) ? Bonus : 0m;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        KirbySprite.SetRobobot(Owner, true);
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        KirbySprite.SetRobobot(oldOwner, false);
        return Task.CompletedTask;
    }

    private bool IsOwnCard(CardModel? card, CardTag tag) =>
        card != null && card.Owner == Owner.Player && card.Tags.Contains(tag);
}
