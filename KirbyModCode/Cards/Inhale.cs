using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Cards;

/// <summary>吸入: 1-cost, Exhaust. Upgrade adds Retain, and the Spit Star / Swallow it gives are upgraded.</summary>
[RegisterCard(typeof(KirbyCardPool))]
[RegisterCharacterStarterCard(typeof(Kirby), 1, Order = 30)]
[RegisterArchaicToothTranscendence(typeof(UltimateInhale))]
public sealed class Inhale() : ModCardTemplate(1, CardType.Skill, CardRarity.Basic, TargetType.AnyEnemy), IInhaleCard
{
    public override CardAssetProfile AssetProfile => KirbyArt.ForCard(GetType());

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => GetInhaleHoverTips(IsUpgraded);

    /// <summary>Shared by every Inhale card: the two powers, then previews of the Spit Star / Swallow it can give.</summary>
    public static IEnumerable<IHoverTip> GetInhaleHoverTips(bool upgraded, bool frenzied = false) =>
    [
        frenzied ? HoverTipFactory.FromPower<FrenziedInhaledPower>() : HoverTipFactory.FromPower<InhaledPower>(),
        HoverTipFactory.FromPower<InhalingPower>(),
        HoverTipFactory.FromCard<SpitStar>(upgraded),
        HoverTipFactory.FromCard<Swallow>(upgraded),
    ];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await KirbyMouth.StartInhale(ctx, Owner, cardPlay.Target!, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
