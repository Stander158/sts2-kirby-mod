using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// 沉睡 (Deep Sleep): each stack makes the next Sleep last 1 more turn (those turns end at once). Waking up (Sleep not
/// lost to damage) then gives, per stack, <see cref="Stats" /> Strength, Dexterity, Vigor (and Stretch on 0.113), heals
/// <see cref="Heal" /> and gives 1 Sweet Dreams. Cleared once that Sleep is over, hurt or not.
/// </summary>
[RegisterPower]
public sealed class KirbySlumberPower : ModPowerTemplate
{
    public const int Stats = 6;
    public const int Heal = 12;

    public override PowerAssetProfile AssetProfile => KirbyArt.Power("slumber");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower(CopyAbilities.CanonicalPower(CopyAbilityKind.Sleep)),
        HoverTipFactory.FromPower<VigorPower>(),
#if STS2_V113
        HoverTipFactory.FromPower<StretchPower>(),
#endif
        HoverTipFactory.FromPower<SweetDreamsPower>(),
    ];

#if !STS2_V113
    // 0.107.1 has no Stretch: the text without it.
    public override LocString Description => new("powers", "KIRBY_MOD_POWER_KIRBY_SLUMBER_POWER.descriptionNoStretch");

    protected override string SmartDescriptionLocKey => "KIRBY_MOD_POWER_KIRBY_SLUMBER_POWER.smartDescriptionNoStretch";
#endif

    /// <summary>The wake-up reward for <paramref name="stacks" /> stacks.</summary>
    public static async Task Reward(PlayerChoiceContext ctx, Creature kirby, int stacks)
    {
        await PowerCmd.Apply<StrengthPower>(ctx, kirby, Stats * stacks, kirby, null);
        await PowerCmd.Apply<DexterityPower>(ctx, kirby, Stats * stacks, kirby, null);
        await CreatureCmd.Heal(kirby, Heal * stacks);
        await PowerCmd.Apply<VigorPower>(ctx, kirby, Stats * stacks, kirby, null);
#if STS2_V113
        await PowerCmd.Apply<StretchPower>(ctx, kirby, Stats * stacks, kirby, null);
#endif
        await PowerCmd.Apply<SweetDreamsPower>(ctx, kirby, stacks, kirby, null);
    }
}

/// <summary>好梦 (from Slumber): for the rest of combat, +Amount energy and +Amount cards at the start of each turn.</summary>
[RegisterPower]
public sealed class SweetDreamsPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("regen_power");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyHandDraw(Player player, decimal count) =>
        player == Owner.Player ? count + Amount : count;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        Flash();
        await PlayerCmd.GainEnergy(Amount, player);
    }
}

/// <summary>篝火: this combat, Burns played while holding Fire deal Amount more damage (see <see cref="FirePower" />).</summary>
[RegisterPower]
public sealed class BonfirePower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("inferno_power");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}

/// <summary>
/// 火焰缠身: whenever a Status enters your discard pile, draw Amount. Outside your play phase (end of turn, enemy turn)
/// the draws are saved for the start of your next turn.
/// </summary>
[RegisterPower]
public sealed class FlameMantlePower : ModPowerTemplate
{
    private sealed class Data
    {
        public int Pending;
    }

    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("flame_barrier_power");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private Data State => GetInternalData<Data>();

    protected override object InitInternalData() => new Data();

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        var player = Owner.Player;
        if (player == null || card.Owner != player || card.Type != CardType.Status || card.Pile?.Type != PileType.Discard
            || oldPileType == PileType.Discard || !Owner.IsAlive)
            return;

        if (player.PlayerCombatState?.Phase != PlayerTurnPhase.Play)
        {
            State.Pending += Amount;
            return;
        }

        Flash();
        await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), Amount, player);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || State.Pending == 0)
            return;

        var draws = State.Pending;
        State.Pending = 0;
        Flash();
        await CardPileCmd.Draw(choiceContext, draws, player);
    }
}

/// <summary>
/// 药瓶 (on an enemy): it lost Amount Strength until the end of its next turn; then it gets it back. Applied only after
/// the Strength loss went through (Artifact can block that), with the amount actually lost.
/// </summary>
[RegisterPower]
public sealed class MedicineBottlePower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("strength_power");

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
            return;

        var amount = Amount;
        Flash();
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, amount, Owner, null);
    }
}
