using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Cards;

/// <summary>
/// 舍弃能力 (0-cost power, Retain): given with every copy ability. Drops the ability (the cards it handed out stay);
/// the starter relic returns Inhale. Losing the last ability any other way exhausts it.
/// </summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class DropAbility() : ModCardTemplate(0, CardType.Power, CardRarity.Token, TargetType.Self)
{
    public override CardAssetProfile AssetProfile => KirbyArt.ForCard(GetType());

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await KirbyMouth.DropCopyAbility(ctx, Owner);
    }
}
