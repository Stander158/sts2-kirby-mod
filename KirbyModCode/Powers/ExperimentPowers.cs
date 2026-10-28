using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>主治医师 (from Experiment): marks who receives the Test Subject's copied cards. No effect of its own.</summary>
[RegisterPower]
public sealed class AttendingPhysicianPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("regen_power");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;
}

/// <summary>
/// 实验对象 (from Experiment), for the rest of combat; stacks. At the start of each player turn (before cards are drawn),
/// 2 random effects per stack happen, all different until every effect has come up once:
/// <list type="bullet">
///     <item>
///         on an enemy: 2 Vulnerable / 2 Weak / 6 Poison / -1 Strength / -8 Strength this turn / heal 20 / stunned
///         this turn (the stun is rare: about 5% to be one of the two);
///     </item>
///     <item>
///         on you or an ally: its first card this turn is copied into the physician's hand / lose 3 HP / 1 Artifact /
///         +1 Strength / +1 Dexterity / +2 energy / 3 Dazed into its hand.
///     </item>
/// </list>
/// </summary>
[RegisterPower]
public sealed class TestSubjectPower : ModPowerTemplate
{
    private enum Effect
    {
        Vulnerable,
        Weak,
        Poison,
        Stun,
        StrengthDown,
        TempStrengthDown,
        EnemyHeal,
        CopyFirstCard,
        LoseHp,
        Artifact,
        StrengthUp,
        DexterityUp,
        Energy,
        Dazed,
    }

    private const int PicksPerStack = 2;

    // Weights: the stun is picked about 5% of the time (one of 2 picks from 6 x 1.0 + 0.15).
    private static readonly (Effect Effect, float Weight)[] EnemyEffects =
    [
        (Effect.Vulnerable, 1f), (Effect.Weak, 1f), (Effect.Poison, 1f), (Effect.StrengthDown, 1f),
        (Effect.TempStrengthDown, 1f), (Effect.EnemyHeal, 1f), (Effect.Stun, 0.15f),
    ];

    private static readonly (Effect Effect, float Weight)[] AllyEffects =
    [
        (Effect.CopyFirstCard, 1f), (Effect.LoseHp, 1f), (Effect.Artifact, 1f), (Effect.StrengthUp, 1f),
        (Effect.DexterityUp, 1f), (Effect.Energy, 1f), (Effect.Dazed, 1f),
    ];

    private sealed class Data
    {
        public Player? Physician;
        public int CardsToCopy;
    }

    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("devour_life_power");

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private Data State => GetInternalData<Data>();

    protected override object InitInternalData() => new Data();

    public Player? Physician
    {
        get => State.Physician;
        set => State.Physician = value;
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        var physician = State.Physician;
        if (physician == null || !Owner.IsAlive)
            return;

        // Enemies roll on the physician's turn, players on their own (before drawing, for the Dazed).
        var trigger = Owner.IsPlayer ? Owner.Player : physician;
        if (player != trigger)
            return;

        var effects = Owner.IsPlayer ? AllyEffects : EnemyEffects;
        var pool = effects.ToList();
        var rng = physician.RunState.Rng.CombatCardGeneration;
        Flash();
        for (var i = 0; i < PicksPerStack * Amount; i++)
        {
            if (pool.Count == 0)
                pool = effects.ToList();

            var roll = rng.NextFloat(pool.Sum(entry => entry.Weight));
            var index = 0;
            while (index < pool.Count - 1 && roll >= pool[index].Weight)
            {
                roll -= pool[index].Weight;
                index++;
            }

            var effect = pool[index].Effect;
            pool.RemoveAt(index);
            Entry.Logger.Info($"[Experiment] {Owner.Monster?.Id.Entry ?? "player"} -> {effect}");
            await Apply(choiceContext, effect, physician);
        }
    }

    private async Task Apply(PlayerChoiceContext ctx, Effect effect, Player physician)
    {
        var source = physician.Creature;
        switch (effect)
        {
            case Effect.Vulnerable:
                await PowerCmd.Apply<VulnerablePower>(ctx, Owner, 2, source, null);
                break;
            case Effect.Weak:
                await PowerCmd.Apply<WeakPower>(ctx, Owner, 2, source, null);
                break;
            case Effect.Poison:
                await PowerCmd.Apply<PoisonPower>(ctx, Owner, 6, source, null);
                break;
            case Effect.Stun:
                await CreatureCmd.Stun(Owner);
                break;
            case Effect.StrengthDown:
                await PowerCmd.Apply<StrengthPower>(ctx, Owner, -1, source, null);
                break;
            case Effect.TempStrengthDown:
                await PowerCmd.Apply<PiercingWailPower>(ctx, Owner, 8, source, null);
                break;
            case Effect.EnemyHeal:
                await CreatureCmd.Heal(Owner, 20);
                break;
            case Effect.CopyFirstCard:
                State.CardsToCopy++;
                break;
            case Effect.LoseHp:
                await CreatureCmd.Damage(ctx, Owner, 3, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
                    source);
                break;
            case Effect.Artifact:
                await PowerCmd.Apply<ArtifactPower>(ctx, Owner, 1, source, null);
                break;
            case Effect.StrengthUp:
                await PowerCmd.Apply<StrengthPower>(ctx, Owner, 1, source, null);
                break;
            case Effect.DexterityUp:
                await PowerCmd.Apply<DexterityPower>(ctx, Owner, 1, source, null);
                break;
            case Effect.Energy:
                await PlayerCmd.GainEnergy(2, Owner.Player!);
                break;
            case Effect.Dazed:
                for (var i = 0; i < 3; i++)
                    await CardPileCmd.AddGeneratedCardToCombat(KirbyMouth.CreateCard<Dazed>(Owner.Player!, false),
                        PileType.Hand, Owner.Player!);
                break;
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var state = State;
        if (state.CardsToCopy == 0 || state.Physician == null || cardPlay.Card.Owner != Owner.Player)
            return;

        var copies = state.CardsToCopy;
        state.CardsToCopy = 0;
        var physician = state.Physician;
        var combat = physician.Creature.CombatState;
        if (combat == null)
            return;

        Flash();
        for (var i = 0; i < copies; i++)
        {
            var copy = combat.CreateCard(ModelDb.GetById<CardModel>(cardPlay.Card.Id), physician);
            if (cardPlay.Card.IsUpgraded && copy.IsUpgradable)
                CardCmd.Upgrade(copy);
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, physician);
        }
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
            State.CardsToCopy = 0;
        return Task.CompletedTask;
    }
}
