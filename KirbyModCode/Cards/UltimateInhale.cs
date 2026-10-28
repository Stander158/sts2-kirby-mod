using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Cards;

/// <summary>
/// 究极吸入 (Ancient frame): Inhale that applies Frenzied Inhaled (dies at 30 HP or less), and until your next turn
/// every hit of the inhaled enemy deals 0. Only obtainable
/// from Archaic Tooth (offered by Orobas, the act 2 ancient), which transcends Inhale into it. As a transcendence card
/// it is skipped by rewards and by Dusty Tome.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class UltimateInhale() : ModCardTemplate(1, CardType.Skill, CardRarity.Ancient, TargetType.AnyEnemy), IInhaleCard
{
    public override CardAssetProfile AssetProfile => KirbyArt.ForCard(GetType());

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate, CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => Inhale.GetInhaleHoverTips(IsUpgraded, frenzied: true);

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await KirbyMouth.StartInhale(ctx, Owner, cardPlay.Target!, this);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
