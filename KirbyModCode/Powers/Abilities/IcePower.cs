using KirbyMod.Characters;
using KirbyMod.Cards;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>冰: when gained and at the start of each turn, turn up to 2 cards in hand into Ice Blocks.</summary>
[RegisterPower]
public sealed class IcePower : CopyAbilityPower
{
    public const int MaxBlocksPerTurn = 2;

    public override CopyAbilityKind Kind => CopyAbilityKind.Ice;

    public override PowerAssetProfile AssetProfile => KirbyArt.Power("ice_ability");

    public override Task OnGained(PlayerChoiceContext ctx, Player player) => Freeze(ctx, player);

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player)
            await Freeze(choiceContext, player);
    }

    private async Task Freeze(PlayerChoiceContext choiceContext, Player player)
    {
        var candidates = PileType.Hand.GetPile(player).Cards.Count(card => card is not IceBlock);
        if (candidates == 0)
            return;

        var chosen = await CardSelectCmd.FromHand(choiceContext, player,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, Math.Min(candidates, MaxBlocksPerTurn)),
            card => card is not IceBlock, this);

        foreach (var card in chosen.ToList())
            await CardCmd.Transform(card, CreateAbilityCard<IceBlock>(player));
    }
}
