using KirbyMod.Cards;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// 火: when gained, add 4 Burns to hand; at the start of each turn, you may turn up to 2 cards in hand into Burns.
/// While held, Burns can be played for free.
/// </summary>
[RegisterPower]
public sealed class FirePower : CopyAbilityPower
{
    public const int BurnsOnGain = 4;
    public const int MaxBurnsPerTurn = 2;
    public const int BurnDamage = 8;
    public const int UpgradedBurnDamageBonus = 4;

    public override CopyAbilityKind Kind => CopyAbilityKind.Fire;

    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("inferno_power");

    public override async Task OnGained(PlayerChoiceContext ctx, Player player)
    {
        var burns = KirbyMouth.LivePiles
            .SelectMany(pile => pile.GetPile(player).Cards)
            .OfType<Burn>()
            .ToList();
        foreach (var burn in burns)
            MakePlayable(burn);

        for (var i = 0; i < BurnsOnGain; i++)
        {
            var burn = KirbyMouth.CreateCard<Burn>(player, upgraded: false);
            await CardPileCmd.AddGeneratedCardToCombat(burn, PileType.Hand, player);
            MakePlayable(burn);
        }
    }

    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (card is Burn && card.Owner == Owner.Player)
            MakePlayable(card);
        return Task.CompletedTask;
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        var applies = card is Burn && card.Owner == Owner.Player;
        modifiedCost = applies ? 0m : originalCost;
        return applies;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card is Burn && cardPlay.Card.Owner == Owner.Player && cardPlay.Target is { } target)
        {
            var damage = BurnDamage + (Upgraded ? UpgradedBurnDamageBonus : 0);
            await DamageCmd.Attack(damage).FromCard(cardPlay.Card).Targeting(target)
                .WithHitFx("vfx/vfx_fire_burst").Execute(choiceContext);
        }
    }

    public override async Task OnLost(PlayerChoiceContext ctx, Player player)
    {
        await base.OnLost(ctx, player);

        foreach (var burn in KirbyMouth.LivePiles
                     .SelectMany(pile => pile.GetPile(player).Cards)
                     .OfType<Burn>())
            burn.AddKeyword(CardKeyword.Unplayable);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        var candidates = PileType.Hand.GetPile(player).Cards.Count(card => card is not Burn);
        if (candidates == 0)
            return;

        var chosen = await CardSelectCmd.FromHand(choiceContext, player,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, Math.Min(candidates, MaxBurnsPerTurn)),
            card => card is not Burn, this);

        foreach (var card in chosen.ToList())
            await Ignite(player, card);
    }

    private async Task Ignite(Player player, CardModel card)
    {
        var burn = KirbyMouth.CreateCard<Burn>(player, upgraded: false);
        await CardCmd.Transform(card, burn);
        MakePlayable(burn);
    }

    private static void MakePlayable(CardModel burn) => burn.RemoveKeyword(CardKeyword.Unplayable);
}
