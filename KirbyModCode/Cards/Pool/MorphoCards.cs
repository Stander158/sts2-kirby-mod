using KirbyMod.Characters;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace KirbyMod.Cards;

/// <summary>
/// 冥蝶 (Morpho): rare skill, 0 cost. Choose a card in the draw pile and add 1(2) Ethereal copies of it to the hand.
/// Then this card costs 1 more for the rest of combat. If it cost 2 or more when played (the third play on), also
/// exhaust every non-Ethereal card in the hand.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class Morpho() : KirbyCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    private const int ExhaustHandCost = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Ethereal), HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var costWhenPlayed = EnergyCost.GetWithModifiers(CostModifiers.All);

        var drawPile = PileType.Draw.GetPile(Owner);
        if (drawPile.Cards.Count > 0)
        {
            var chosen = (await CardSelectCmd.FromCombatPile(ctx, drawPile, Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
            if (chosen != null)
                for (var i = 0; i < DynamicVars.Cards.IntValue; i++)
                {
                    var copy = chosen.CreateClone();
                    copy.AddKeyword(CardKeyword.Ethereal);
                    await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
                }
        }

        EnergyCost.AddThisCombat(1);
        if (costWhenPlayed < ExhaustHandCost)
            return;

        foreach (var card in PileType.Hand.GetPile(Owner).Cards
                     .Where(card => !card.Keywords.Contains(CardKeyword.Ethereal)).ToList())
            await CardCmd.Exhaust(ctx, card);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

/// <summary>邪恶形态 (Possessed Form): rare power, 3(2). Whenever you deal unblocked damage with an Attack, draw 1.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class PossessedForm() : KirbyCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay) =>
        await PowerCmd.Apply<PossessedFormPower>(ctx, Owner.Creature, DynamicVars.Cards.BaseValue, Owner.Creature,
            this);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>
/// 能力星星温存 (Ability Star Saver): rare power, 1. Whenever you lose a copy ability (except Sleep waking up), shuffle
/// an Ethereal copy of its ability card into the draw pile and put 1(2) cards from the discard pile into your hand.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class AbilityStarSaver() : KirbyCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Ethereal)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay) =>
        await PowerCmd.Apply<AbilityStarSaverPower>(ctx, Owner.Creature, DynamicVars.Cards.BaseValue, Owner.Creature,
            this);

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}
