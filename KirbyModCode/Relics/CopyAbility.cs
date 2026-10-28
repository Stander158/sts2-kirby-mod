using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Relics;

/// <summary>
/// Kirby's starter relic and its Touch of Orobas upgrade. Inhale cards are Innate (given at the first hand draw of each
/// combat, before the game picks the Innate cards), and whenever an attack makes Kirby lose a copy ability (see
/// <see cref="CopyAbilities.OnHeavyHit" />), Inhale returns from the exhaust pile to hand.
/// </summary>
public abstract class CopyAbilityRelicBase : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => KirbyArt.Relic("ability_essence");

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != Owner || player.PlayerCombatState?.TurnNumber != 1)
            return Task.CompletedTask;

        foreach (var card in PileType.Draw.GetPile(player).Cards.Where(card => card is IInhaleCard))
            card.AddKeyword(CardKeyword.Innate);
        return Task.CompletedTask;
    }

    public async Task AfterAbilityLostToAttack()
    {
        if (await KirbyMouth.PutInhaleIntoHand(Owner))
            Flash();
    }
}

/// <summary>复制能力 (starter).</summary>
[RegisterRelic(typeof(KirbyRelicPool))]
[RegisterCharacterStarterRelic(typeof(Kirby), Order = 0)]
[RegisterTouchOfOrobasRefinement(typeof(CopyEssenceDeluxe))]
public sealed class CopyAbility : CopyAbilityRelicBase;

/// <summary>豪华复制精华 (Touch of Orobas upgrade of Copy Ability): abilities are only lost to hits of 12 or more HP.</summary>
[RegisterRelic(typeof(KirbyRelicPool))]
public sealed class CopyEssenceDeluxe : CopyAbilityRelicBase
{
    public const int HeavyHit = 12;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("HpLoss", HeavyHit)];
}
