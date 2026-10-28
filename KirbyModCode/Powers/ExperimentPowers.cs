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
/// 实验对象 (from Experiment), for the rest of combat; stacks, and several players can each be its physician. At the
/// start of each player turn (before cards are drawn), each physician rolls 2 random effects per stack they put on it
/// (every roll is independent, so effects can repeat):
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

    // Weights: each roll is the stun about 2.4% of the time (0.15 out of 6 x 1.0 + 0.15).
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
        /// <summary>Each physician and how many stacks they applied, in the order they first applied one.</summary>
        public readonly List<(Player Physician, int Stacks)> Physicians = [];
        public readonly Dictionary<Player, int> CardsToCopy = [];
    }

    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("devour_life_power");

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private Data State => GetInternalData<Data>();

    protected override object InitInternalData() => new Data();

    /// <summary>Records one stack put on by <paramref name="physician" /> (the power's amount is added separately).</summary>
    public void AddPhysicianStack(Player physician)
    {
        var physicians = State.Physicians;
        var index = physicians.FindIndex(entry => entry.Physician == physician);
        if (index < 0)
            physicians.Add((physician, 1));
        else
            physicians[index] = (physician, physicians[index].Stacks + 1);
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (!Owner.IsAlive)
            return;

        // Enemies roll on each physician's turn, players on their own (before drawing, for the Dazed).
        var rolling = Owner.IsPlayer
            ? player == Owner.Player ? State.Physicians.ToList() : []
            : State.Physicians.Where(entry => entry.Physician == player).ToList();
        if (rolling.Count == 0)
            return;

        var effects = Owner.IsPlayer ? AllyEffects : EnemyEffects;
        var totalWeight = effects.Sum(entry => entry.Weight);
        Flash();
        foreach (var (physician, stacks) in rolling)
        {
            var rng = physician.RunState.Rng.CombatCardGeneration;
            for (var i = 0; i < PicksPerStack * stacks; i++)
            {
                var roll = rng.NextFloat(totalWeight);
                var index = 0;
                while (index < effects.Length - 1 && roll >= effects[index].Weight)
                {
                    roll -= effects[index].Weight;
                    index++;
                }

                var effect = effects[index].Effect;
                Entry.Logger.Info($"[Experiment] {Owner.Monster?.Id.Entry ?? "player"} -> {effect}");
                await Apply(choiceContext, effect, physician);
            }
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
                State.CardsToCopy[physician] = State.CardsToCopy.GetValueOrDefault(physician) + 1;
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
        var pending = State.CardsToCopy;
        if (pending.Count == 0 || cardPlay.Card.Owner != Owner.Player)
            return;

        var copies = pending.ToList();
        pending.Clear();
        Flash();
        foreach (var (physician, count) in copies)
        {
            var combat = physician.Creature.CombatState;
            if (combat == null)
                continue;

            for (var i = 0; i < count; i++)
            {
                var copy = combat.CreateCard(ModelDb.GetById<CardModel>(cardPlay.Card.Id), physician);
                if (cardPlay.Card.IsUpgraded && copy.IsUpgradable)
                    CardCmd.Upgrade(copy);
                await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, physician);
            }
        }
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
            State.CardsToCopy.Clear();
        return Task.CompletedTask;
    }
}
