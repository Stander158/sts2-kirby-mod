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

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource) =>
        IsOwnCard(cardSource, CardTag.Strike) && props.IsPoweredAttack() ? Bonus : 0m;

    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource,
        CardPlay? cardPlay) =>
        IsOwnCard(cardSource, CardTag.Defend) ? Bonus : 0m;

    private bool IsOwnCard(CardModel? card, CardTag tag) =>
        card != null && card.Owner == Owner.Player && card.Tags.Contains(tag);
}
