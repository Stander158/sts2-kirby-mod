using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Relics;

/// <summary>
/// Kirby's starter relic and its Touch of Orobas upgrade: losing <see cref="HpLossThreshold" /> or more HP to a single
/// enemy attack while holding a copy ability strips it (exhausting its cards) and returns Inhale from the exhaust pile
/// to hand. With Ability Conservation, 1 stack is spent instead. Without an ability nothing happens. Buffer and the
/// like prevent the HP loss, so they also keep the ability.
/// </summary>
public abstract class CopyAbilityRelicBase : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => KirbyArt.Relic("ability_essence");

    public abstract int HpLossThreshold { get; }

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("HpLoss", HpLossThreshold)];

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner.Creature || result.UnblockedDamage < HpLossThreshold || !props.IsPoweredAttack()
            || dealer == null || dealer.Side == target.Side)
            return;

        // Ability Conservation spends a stack instead (only when there is an ability to keep).
        if (Owner.Creature.GetPower<AbilityConservationPower>() is { } conservation)
        {
            if (CopyAbilities.HasAny(Owner))
            {
                Flash();
                await PowerCmd.Decrement(conservation);
            }

            return;
        }

        if (!await KirbyMouth.LoseCopyAbility(choiceContext, Owner))
            return;

        Flash();
        await KirbyMouth.PutInhaleIntoHand(Owner);
    }
}

/// <summary>复制能力 (starter): the ability is lost to an attack that takes 6 or more HP.</summary>
[RegisterRelic(typeof(KirbyRelicPool))]
[RegisterCharacterStarterRelic(typeof(Kirby), Order = 0)]
[RegisterTouchOfOrobasRefinement(typeof(CopyEssenceDeluxe))]
public sealed class CopyAbility : CopyAbilityRelicBase
{
    public override int HpLossThreshold => 6;
}

/// <summary>豪华复制精华 (Touch of Orobas upgrade of Copy Ability): the threshold becomes 12 or more HP.</summary>
[RegisterRelic(typeof(KirbyRelicPool))]
public sealed class CopyEssenceDeluxe : CopyAbilityRelicBase
{
    public override int HpLossThreshold => 12;
}
