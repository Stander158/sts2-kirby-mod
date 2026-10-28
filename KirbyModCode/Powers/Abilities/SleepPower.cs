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
/// 睡眠: on gain, end the turn immediately. At the start of the next turn Sleep ends (other abilities stay); if Kirby
/// lost no HP in between, gain 2(3) Strength, 2(3) Dexterity and heal 8(12) for each time it was gained (twice under
/// Super Ability).
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

    public override Task OnGained(PlayerChoiceContext ctx, Player player)
    {
        State.TimesGained++;
        return Task.CompletedTask;
    }

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && result.UnblockedDamage > 0)
            State.WasHurt = true;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        if (!State.WasHurt)
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, StatGain, Owner, null);
            await PowerCmd.Apply<DexterityPower>(choiceContext, Owner, StatGain, Owner, null);
            await CreatureCmd.Heal(Owner, (Upgraded ? UpgradedHeal : Heal) * TimesGained);
        }

        await KirbyMouth.LoseAbility(choiceContext, player, this);
    }
}
