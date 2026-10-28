using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// 吸入 (on Kirby), until Kirby's next turn. Succeeds at most once, in the first of these ways:
/// <list type="bullet">
///     <item>
///         the inhaled enemy's first attack on Kirby gives Mouthful equal to the damage of all its hits. The hits
///         are taken in full, unless Kirby has Ultimate Fruit: then that attack deals 0;
///     </item>
///     <item>the first status card the inhaled enemy gives Kirby is removed, for 6 Mouthful (a Burn means Fire);</item>
///     <item>the inhaled enemy dies, for Mouthful equal to half its Max HP;</item>
///     <item>otherwise, 6 Mouthful at the start of Kirby's next turn.</item>
/// </list>
/// From Hypernova Inhale, every hit of the inhaled enemy deals half damage until then (the first attack still becomes
/// Mouthful at its full damage).
/// </summary>
[RegisterPower]
public sealed class InhalingPower : ModPowerTemplate
{
    private sealed class Data
    {
        public Creature? Target;
        public InhaleOutcome Outcome;
        public bool Resolved;
        public bool UpgradedTokens;

        // Hypernova Inhale: all damage from the inhaled enemy is halved.
        public bool NegateAll;

        // Set during the inhaled enemy's first attack; its hits on Kirby add up to the Mouthful.
        public bool Tracking;
        public bool Hit;
        public decimal Total;

        // Ultimate Fruit: set while the inhaled enemy is attacking, so damage previews (intent numbers) stay unmodified.
        public bool NegatingAttack;
        public decimal PendingBaseDamage;
        public ValueProp PendingProps;
    }

    public override PowerAssetProfile AssetProfile => KirbyArt.Power("hover");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    private Data State => GetInternalData<Data>();

    protected override object InitInternalData() => new Data();

    public void Begin(Creature target, InhaleOutcome outcome, bool upgradedTokens, bool negateAll)
    {
        State.Target = target;
        State.Outcome = outcome;
        State.UpgradedTokens = upgradedTokens;
        State.NegateAll = negateAll;
    }

    /// <summary>
    /// A hit of the inhaled enemy's first attack on Kirby: <paramref name="amount" /> is its final damage (after
    /// strength, vulnerable, ...; before block), added to the Mouthful. The hit itself is not changed. (With Ultimate
    /// Fruit the hit was already capped to 0 and counted in <see cref="AfterModifyingDamageAmount" />.)
    /// </summary>
    public override Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        var state = State;
        if (!state.Tracking || state.NegatingAttack || target != Owner || dealer != state.Target
            || !props.IsPoweredAttack())
            return Task.CompletedTask;

        state.Hit = true;
        state.Total += state.NegateAll ? amount * 2 : amount;  // Mouthful counts the hit before it was halved
        return Task.CompletedTask;
    }

    // ---- Ultimate Fruit / Ultimate Inhale: the first hit deals 0 (the damage it would have dealt still becomes
    // Mouthful). Ultimate Inhale also zeroes every later hit of the inhaled enemy. ----

    public override Task BeforeAttack(AttackCommand command)
    {
        var state = State;
        if (state.Resolved || state.Outcome != InhaleOutcome.FirstHit || command.Attacker != state.Target
            || !command.DamageProps.IsPoweredAttack())
            return Task.CompletedTask;

        state.Tracking = true;
        state.Hit = false;
        state.Total = 0m;

        if (!Owner.HasPower<UltimateFruitPower>())
            return Task.CompletedTask;

        state.NegatingAttack = true;
        state.PendingProps = command.DamageProps;
        state.PendingBaseDamage = AttackMath.BaseDamage(command, Owner);
        return Task.CompletedTask;
    }

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        var state = State;
        if (command.Attacker != state.Target)
            return;

        state.NegatingAttack = false;
        if (!state.Tracking)
            return;

        // An attack that never hit Kirby (e.g. it targeted an ally) leaves the inhale waiting.
        state.Tracking = false;
        if (!state.Hit || state.Resolved)
            return;

        state.Resolved = true;
        Flash();
        await KirbyMouth.Succeed(choiceContext, Owner, (int)state.Total, state.UpgradedTokens,
            "inhaled enemy's first attack (all hits)");
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        var state = State;
        return state.NegateAll && target == Owner && dealer != null && dealer == state.Target ? 0.5m : 1m;
    }

    public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        var state = State;
        if (target != Owner || dealer == null || dealer != state.Target)
            return decimal.MaxValue;

        return state.NegatingAttack && state.Tracking && props.IsPoweredAttack()
            ? 0m
            : decimal.MaxValue;
    }

    /// <summary>Only called when our cap actually zeroed a real hit (never for previews).</summary>
    public override Task AfterModifyingDamageAmount(CardModel? cardSource)
    {
        var state = State;
        if (!state.NegatingAttack || !state.Tracking || state.Target == null)
            return Task.CompletedTask;

        // The hit would have dealt this much; it still counts toward the Mouthful (resolved in AfterAttack).
        state.Hit = true;
        state.Total += AttackMath.UncappedDamage(Owner, state.Target, state.PendingBaseDamage, state.PendingProps,
            cardSource);
        return Task.CompletedTask;
    }

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        var state = State;
        if (state.Resolved || state.Outcome != InhaleOutcome.BlockFirstStatusCard || card.Type != CardType.Status
            || card.Owner != Owner.Player || state.Target?.Monster?.IsPerformingMove != true)
            return;

        state.Resolved = true;
        if (card is Burn && Owner.GetPower<AbilityInMouthPower>() is { } inMouth)
            inMouth.Kind = CopyAbilityKind.Fire;

        await CardPileCmd.RemoveFromCombat(card);
        Flash();
        await KirbyMouth.Succeed(new BlockingPlayerChoiceContext(), Owner, KirbyMouth.FallbackMouthful,
            state.UpgradedTokens, $"blocked status card {card.Id.Entry}");
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented,
        float deathAnimLength)
    {
        var state = State;
        if (state.Resolved || wasRemovalPrevented || creature != state.Target)
            return;

        state.Resolved = true;
        Flash();
        await KirbyMouth.Succeed(choiceContext, Owner, Math.Max(1, creature.MaxHp / 2), state.UpgradedTokens,
            $"inhaled enemy died, max hp {creature.MaxHp}");
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
            return;

        if (!State.Resolved)
        {
            State.Resolved = true;
            await KirbyMouth.Succeed(choiceContext, Owner, KirbyMouth.FallbackMouthful, State.UpgradedTokens,
                "fallback at turn start");
        }

        await PowerCmd.Remove(this);
    }
}
