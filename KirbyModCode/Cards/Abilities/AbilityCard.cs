using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace KirbyMod.Cards;

/// <summary>
/// A "gain copy ability X" card: a 1-cost skill unless noted (Stone is a 2-cost power, Star Rod / Galaxia cost 2).
/// Upgrading lowers the cost by 1 only for the 2-cost ones (1-cost cards keep their cost), and an upgraded card
/// hands out an Uncommon-or-better card of the ability instead of a Common one. It is exhausted, or discarded (playable again) while Galaxy Wish is active.
/// </summary>
public abstract class AbilityCard(CardRarity rarity, int cost = 1, CardType type = CardType.Skill)
    : KirbyCard(cost, type, rarity, TargetType.Self)
{
    public abstract CopyAbilityKind Kind { get; }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower(CopyAbilities.CanonicalPower(Kind))];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CopyAbilities.Gain(ctx, Owner, Kind, IsUpgraded, this);
    }

    protected override void OnUpgrade()
    {
        if (EnergyCost.Canonical > 1)
            EnergyCost.UpgradeBy(-1);
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        Owner.Creature.HasPower<GalaxyWishPower>() ? PileType.Discard : PileType.Exhaust;
}
