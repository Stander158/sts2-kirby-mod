using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Cards;

/// <summary>吞咽 (0-cost power): empty the mouth and gain the ability in it; its Drop Ability goes to the draw pile. Upgraded: Drop Ability to hand, and draw 1 card.</summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class Swallow() : ModCardTemplate(0, CardType.Power, CardRarity.Token, TargetType.Self)
{
    public override CardAssetProfile AssetProfile => KirbyArt.ForCard(GetType());

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<AbilityInMouthPower>(), HoverTipFactory.FromCard<DropAbility>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var kirby = Owner.Creature;

        var inMouth = kirby.GetPower<AbilityInMouthPower>();
        await KirbyMouth.ClearMouth(kirby, keepMouthful: kirby.HasPower<BubbleStoragePower>());

        if (inMouth != null)
            await CopyAbilities.Gain(ctx, Owner, inMouth.Kind, upgraded: false, this,
                dropAbilityPile: IsUpgraded ? PileType.Hand : PileType.Draw);

        await KirbyMouth.ExhaustOtherMouthCards(ctx, Owner, this);

        if (IsUpgraded)
            await CardPileCmd.Draw(ctx, 1, Owner);
    }

    // Upgrade moves the Drop Ability to hand and adds a draw (see OnPlay).
    protected override void OnUpgrade()
    {
    }
}
