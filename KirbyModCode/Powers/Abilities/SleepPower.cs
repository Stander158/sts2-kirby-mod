using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// 睡眠: on gain, end the turn immediately. Any unblocked damage (from any source, not the round total) loses it, unless
/// Ability Conservation takes the hit. At the start of the next turn Kirby wakes up with it still held: Sleep ends
/// (other abilities stay) and he gains 2(3) Strength, 2(3) Dexterity and heals 8(12) for each time it was gained
/// (doubled / tripled under Super Ability). With Slumber it lasts 1 more turn per stack and waking also gives the
/// Slumber reward (<see cref="KirbySlumberPower" />); Slumber is cleared once this Sleep is over.
/// </summary>
[RegisterPower]
public sealed class SleepPower : CopyAbilityPower
{
    public const int Heal = 8;
    public const int UpgradedHeal = 12;
    public const int Stats = 2;

    public override CopyAbilityKind Kind => CopyAbilityKind.Sleep;

    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("asleep_power");

    private int TimesGained => Math.Max(1, State.TimesGained);

    private int StatGain => (Upgraded ? Stats + 1 : Stats) * TimesGained;

    public override bool EndsTurnOnGain => true;

    // Lost to any unblocked hit instead (below).
    public override bool LostToHeavyHits => false;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        KirbyFx.PlaySound(KirbyFx.SleepSound);
        return base.AfterApplied(applier, cardSource);
    }

    public override Task OnGained(PlayerChoiceContext ctx, Player player)
    {
        State.TimesGained += GainMultiplier;
        var slumber = Owner.GetPowerAmount<KirbySlumberPower>();
        State.Slumber = slumber;
        State.ExtraSleepTurns = slumber;
        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // Still counts the round's damage for the other abilities held (Smash Bros. Kirby).
        await base.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource);
        if (target != Owner || result.UnblockedDamage <= 0 || Owner.Player is not { } player
            || !Owner.Powers.Contains(this))
            return;

        if (await CopyAbilities.SpendConservation(Owner, result))
            return;

        await KirbyMouth.LoseAbility(choiceContext, player, this);
        await CopyAbilities.AfterAbilityLost(player);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        // Slumber: these turns are slept through.
        if (State.ExtraSleepTurns > 0)
        {
            State.ExtraSleepTurns--;
            Flash();
            PlayerCmd.EndTurn(player, canBackOut: false);
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, StatGain, Owner, null);
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner, StatGain, Owner, null);
        await CreatureCmd.Heal(Owner, (Upgraded ? UpgradedHeal : Heal) * TimesGained);
        if (State.Slumber > 0)
            await KirbySlumberPower.Reward(choiceContext, Owner, State.Slumber);

        await KirbyMouth.LoseAbility(choiceContext, player, this);
    }

    // Slumber is used up by this Sleep, whether it ends by waking or early.
    public override async Task OnLost(PlayerChoiceContext ctx, Player player)
    {
        await base.OnLost(ctx, player);
        if (State.Slumber > 0 && Owner.GetPower<KirbySlumberPower>() is { } slumber)
            await PowerCmd.Remove(slumber);
        State.Slumber = 0;
        State.ExtraSleepTurns = 0;
    }
}
