using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Cards;

/// <summary>扫描 (made by Robobot from Inhale): a 0-cost Inhale. Exhaust; upgrade adds Retain.</summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class Scan() : ModCardTemplate(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy), IInhaleCard
{
    public override CardAssetProfile AssetProfile => KirbyArt.ForCard(GetType());

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => Inhale.GetInhaleHoverTips(IsUpgraded);

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await KirbyMouth.StartInhale(ctx, Owner, cardPlay.Target!, this);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
