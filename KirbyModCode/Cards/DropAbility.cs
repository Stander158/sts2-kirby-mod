using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Cards;

/// <summary>
/// 舍弃能力 (0-cost power, Retain): given with every copy ability. Drops the ability (exhausting its cards); the
/// starter relic returns Inhale. It counts as one of the ability's cards, so losing the ability any
/// other way exhausts it too.
/// </summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class DropAbility() : ModCardTemplate(0, CardType.Power, CardRarity.Token, TargetType.Self), ICopyAbilityCard
{
    public override CardAssetProfile AssetProfile => KirbyArt.ForCard(GetType());

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    public CopyAbilityKind? GrantedBy => null;

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await KirbyMouth.DropCopyAbility(ctx, Owner);
    }
}
