using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// 邪恶形态 (Possessed Form): whenever the owner deals unblocked damage with an Attack card, draw Amount cards. Every
/// hit counts (multi-hit attacks and each enemy hit by an all-enemy attack); fully blocked hits do not.
/// </summary>
[RegisterPower]
public sealed class PossessedFormPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyArt.Power("possessed_form");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner || cardSource?.Type != CardType.Attack || result.UnblockedDamage <= 0
            || Owner.Player is not { } player)
            return;

        Flash();
        await CardPileCmd.Draw(choiceContext, Amount, player);
    }
}

/// <summary>
/// 能力星星温存 (Ability Star Saver): whenever a copy ability is lost (not Sleep waking up), shuffle an Ethereal copy of
/// its ability card (upgraded if the ability was) into the draw pile, then put Amount cards from the discard pile
/// into the hand. Abilities without an ability card (Void) only give the discard pick. Several abilities lost at
/// once (Smash Bros. Kirby) trigger it once each. Called from <see cref="KirbyMouth" />.
/// </summary>
[RegisterPower]
public sealed class AbilityStarSaverPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyArt.Power("ability_star_saver");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public static async Task OnAbilityLost(PlayerChoiceContext ctx, Player player, CopyAbilityPower lost)
    {
        if (player.Creature.GetPower<AbilityStarSaverPower>() is { } saver)
            await saver.Trigger(ctx, player, lost.Kind, lost.Upgraded);
    }

    private async Task Trigger(PlayerChoiceContext ctx, Player player, CopyAbilityKind kind, bool upgraded)
    {
        Flash();
        if (CopyAbilities.HasAbilityCard(kind))
        {
            var card = CopyAbilities.CreateAbilityCard(player, kind);
            if (upgraded && card.IsUpgradable)
                CardCmd.Upgrade(card);
            card.AddKeyword(CardKeyword.Ethereal);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, player, CardPilePosition.Random);
        }

        var discard = PileType.Discard.GetPile(player);
        var count = Math.Min(Amount, discard.Cards.Count);
        if (count <= 0)
            return;

        var chosen = await CardSelectCmd.FromCombatPile(ctx, discard, player,
            new CardSelectorPrefs(SelectionScreenPrompt, count));
        foreach (var card in chosen.ToList())
            await CardPileCmd.Add(card, PileType.Hand);
    }
}
