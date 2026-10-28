using KirbyMod.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>Base for Frost's this-turn Ice Block bonuses; removed at the end of your turn.</summary>
public abstract class FrostBonusPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
            await PowerCmd.Remove(this);
    }
}

/// <summary>霜冻 (damage): this turn, your Ice Blocks deal +Amount damage.</summary>
[RegisterPower]
public sealed class FrostDamagePower : FrostBonusPower
{
    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("strength_power");

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource) =>
        cardSource is IceBlock && dealer == Owner ? Amount : 0m;
}

/// <summary>霜冻 (Block): this turn, your Ice Blocks give +Amount Block.</summary>
[RegisterPower]
public sealed class FrostBlockPower : FrostBonusPower
{
    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("dexterity_power");

    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource,
        CardPlay? cardPlay) =>
        cardSource is IceBlock && target == Owner ? Amount : 0m;
}

/// <summary>究极冰球: your Ice Blocks deal damage and give Block, without choosing.</summary>
[RegisterPower]
public sealed class SnowBowlPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("barricade_power");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;
}
