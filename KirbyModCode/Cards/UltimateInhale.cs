using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Cards;

/// <summary>
/// 超新星吸入 (Hypernova Inhale; class name kept from 究极吸入) (Ancient frame): Inhale that swallows whole at 33 HP or less, and until your next turn
/// every hit of the inhaled enemy deals half damage. Only obtainable
/// from Archaic Tooth (offered by Orobas, the act 2 ancient), which transcends Inhale into it. As a transcendence card
/// it is skipped by rewards and by Dusty Tome.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class UltimateInhale() : ModCardTemplate(1, CardType.Skill, CardRarity.Ancient, TargetType.AnyEnemy), IInhaleCard
{
    public override CardAssetProfile AssetProfile => KirbyArt.ForCard(GetType());

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <summary>The Inhale tips, with 压制 (Suppressed: the inhaled enemy's damage to you is halved) after the first.</summary>
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            var tips = Inhale.GetInhaleHoverTips(IsUpgraded, KirbyMouth.FrenziedExecuteThreshold).ToList();
            tips.Insert(1, new HoverTip(new LocString("powers", "KIRBY_MOD_SUPPRESSED.title"),
                new LocString("powers", "KIRBY_MOD_SUPPRESSED.description")));
            return tips;
        }
    }

    protected override bool IsPlayable => KirbyMouth.CanUseInhale(Owner);

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await KirbyMouth.StartInhale(ctx, Owner, cardPlay.Target!, this);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
