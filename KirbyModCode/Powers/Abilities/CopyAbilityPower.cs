using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// One of Kirby's copy abilities (see <see cref="CopyAbilities.Gain" />). While held, synergy cards for
/// <see cref="Kind" /> in hand are retained at end of turn. Taking <see cref="CopyAbilities.HeavyHitThreshold" /> or more
/// damage from enemies within one round makes Kirby lose it (see <see cref="CopyAbilities.OnHeavyHit" />), unless
/// <see cref="LostToHeavyHits" /> is false.
/// </summary>
public abstract class CopyAbilityPower : ModPowerTemplate
{
    protected sealed class Data
    {
        public bool Upgraded;

        /// <summary>Buffs this ability granted (power type → amount), taken back when it is lost.</summary>
        public readonly Dictionary<Type, int> Granted = [];

        /// <summary>Sleep: Kirby lost HP since the ability was gained.</summary>
        public bool WasHurt;

        /// <summary>Sleep: how many times it was gained before waking up (2 under Super Ability).</summary>
        public int TimesGained;
    }

    public abstract CopyAbilityKind Kind { get; }

    /// <summary>The abilities this one counts as for "X: ..." card effects and Retain (Master also counts as Sword).</summary>
    public virtual IReadOnlyList<CopyAbilityKind> CountsAs => [Kind];

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>Gained from an upgraded ability card (Ice, Fire and Sleep get stronger).</summary>
    public bool Upgraded
    {
        get => GetInternalData<Data?>()?.Upgraded ?? false;
        set => GetInternalData<Data>().Upgraded = value;
    }

    /// <summary>Sleep: gaining it ends the turn (done once by <see cref="CopyAbilities.Gain" />, even when gained twice).</summary>
    public virtual bool EndsTurnOnGain => false;

    /// <summary>Whether a heavy hit makes Kirby lose this ability (Star Rod: no).</summary>
    public virtual bool LostToHeavyHits => true;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        var player = Owner.Player;
        if (player == null || target != Owner || dealer == null || dealer.Side == target.Side || result.UnblockedDamage <= 0)
            return;

        // With several abilities held (Smash Bros. Kirby), the first one counts the damage for all of them.
        if (CopyAbilities.Held(player).FirstOrDefault() != this)
            return;

        if (CopyAbilities.AddRoundDamage(player, result.UnblockedDamage))
            await CopyAbilities.OnHeavyHit(choiceContext, player);
    }

    protected Data State => GetInternalData<Data>();

    protected override object InitInternalData() => new Data();

    /// <summary>On-gain effect. Runs again when a held ability is regained (and twice under Super Ability).</summary>
    public virtual Task OnGained(PlayerChoiceContext ctx, Player player) => Task.CompletedTask;

    /// <summary>
    /// Takes back everything <see cref="Grant{TPower}" /> gave: stats (Strength, Dexterity) in full, consumable buffs
    /// (Vigor, Buffer) only as far as they are still unused.
    /// </summary>
    public virtual async Task OnLost(PlayerChoiceContext ctx, Player player)
    {
        foreach (var (powerType, amount) in State.Granted)
        {
            var current = Owner.Powers.FirstOrDefault(power => power.GetType() == powerType);
            if (current == null)
                continue;

            var revoke = current.AllowNegative ? amount : Math.Min(current.Amount, amount);
            if (revoke != 0)
                await PowerCmd.ModifyAmount(ctx, current, -revoke, Owner, null);
        }

        State.Granted.Clear();
    }

    /// <summary>Applies a buff that lasts only while this ability is held (see <see cref="OnLost" />).</summary>
    protected async Task Grant<TPower>(PlayerChoiceContext ctx, int amount) where TPower : PowerModel
    {
        if (amount == 0)
            return;

        await PowerCmd.Apply<TPower>(ctx, Owner, amount, Owner, null);
        State.Granted[typeof(TPower)] = State.Granted.GetValueOrDefault(typeof(TPower)) + amount;
    }

    /// <summary>Creates a card this ability makes (Ice Block, Burn): upgraded if the ability is.</summary>
    protected TCard CreateAbilityCard<TCard>(Player player) where TCard : CardModel =>
        KirbyMouth.CreateCard<TCard>(player, Upgraded);

    public override Task BeforeFlush(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return Task.CompletedTask;

        foreach (var card in PileType.Hand.GetPile(player).Cards)
        {
            if (card is IAbilitySynergyCard synergy && synergy.SynergyAbilities.Any(CountsAs.Contains))
                card.GiveSingleTurnRetain();
        }

        return Task.CompletedTask;
    }
}
