using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Cards;

/// <summary>
/// 草莓小蛋糕 (Strawberry Shortcake): rare skill, X cost, Exhaust. Shuffle X Shortcake Slices (Slice+ when upgraded)
/// into the draw pile at random positions, then draw X.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class StrawberryShortcake() : KirbyCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<ShortcakeSlice>(IsUpgraded)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var x = ResolveEnergyXValue();
        for (var i = 0; i < x; i++)
            await CardPileCmd.AddGeneratedCardToCombat(KirbyMouth.CreateCard<ShortcakeSlice>(Owner, IsUpgraded),
                PileType.Draw, Owner, CardPilePosition.Random);
        if (x > 0)
            await CardPileCmd.Draw(ctx, x, Owner);
    }
}

/// <summary>草莓小蛋糕块 (Shortcake Slice): token skill, 0 cost, Exhaust. Gain 1 energy, draw 1(2).</summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class ShortcakeSlice() : ModCardTemplate(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override CardAssetProfile AssetProfile => KirbyArt.ForCard(GetType());

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1), new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}
