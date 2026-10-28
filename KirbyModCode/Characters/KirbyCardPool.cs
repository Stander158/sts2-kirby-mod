using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace KirbyMod.Characters;

public sealed class KirbyCardPool : TypeListCardPoolModel
{
    private static readonly Material FrameTint =
        MaterialUtils.CreateReplaceHueShaderMaterial(KirbyTheme.Pink.R, KirbyTheme.Pink.G, KirbyTheme.Pink.B);

    private static readonly CardType[] RequiredTypes = [CardType.Attack, CardType.Skill, CardType.Power];
    private static readonly CardRarity[] RequiredRarities = [CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare];

    public override string Title => "kirbymod";

    public override string EnergyColorName => KirbyTheme.EnergyColorName;

    public override string? BigEnergyIconPath => KirbyTheme.BigEnergyIcon;

    public override string? TextEnergyIconPath => KirbyTheme.TextEnergyIcon;

    public override Color DeckEntryCardColor => KirbyTheme.Pink;

    public override Color EnergyOutlineColor => KirbyTheme.PinkDark;

    public override Material? PoolFrameMaterial => FrameTint;

    public override bool IsColorless => false;

    /// <summary>
    /// TEMPORARY (until DESIGN.md phase 4): while Kirby's own pool lacks a non-Basic card of some type or rarity,
    /// everything that draws from it (card rewards, the shop, transforms, random-card relics) also gets the Ironclad's
    /// unlocked cards. Without this the game throws on e.g. Neow's Hefty Tablet. Switches itself off once the pool
    /// covers every type and rarity.
    /// </summary>
    protected override IEnumerable<CardModel> FilterThroughEpochs(UnlockState unlockState, IEnumerable<CardModel> cards)
    {
        var own = base.FilterThroughEpochs(unlockState, cards).ToList();
        if (HasRewardCoverage(own))
            return own;

        var borrowed = ModelDb.CardPool<IroncladCardPool>()
            .GetUnlockedCards(unlockState, CardMultiplayerConstraint.None);
        return [.. own, .. borrowed];
    }

    private static bool HasRewardCoverage(IReadOnlyCollection<CardModel> cards)
    {
        var rewardable = cards.Where(card => card.Rarity != CardRarity.Basic).ToList();
        return RequiredTypes.All(type => rewardable.Any(card => card.Type == type))
               && RequiredRarities.All(rarity => rewardable.Any(card => card.Rarity == rarity));
    }
}
