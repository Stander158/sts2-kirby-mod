using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.CardTargeting;
using STS2RitsuLib.Interop.AutoRegistration;

namespace KirbyMod.Cards;

/// <summary>
/// 超速居合: costs 1 more each turn after the first. Deals 37(50) only if Kirby has not lost HP this combat; on turn 1
/// it also applies 2(4) Weak and 2(4) Vulnerable (after the hit). Exhaust.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class QuickDraw() : KirbyCard(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(37, ValueProp.Move), new PowerVar<WeakPower>(2), new PowerVar<VulnerablePower>(2)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<VulnerablePower>()];

    private bool Untouched => !CombatManager.Instance.History.Entries.OfType<DamageReceivedEntry>()
        .Any(e => e.Receiver == Owner.Creature && e.Result.UnblockedDamage > 0);

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card != this || CombatState == null || CombatState.RoundNumber <= 1)
            return false;

        modifiedCost = originalCost + CombatState.RoundNumber - 1;
        return true;
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (Untouched)
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(ctx);

        if (CombatState?.RoundNumber == 1 && target.IsAlive)
        {
            await PowerCmd.Apply<WeakPower>(ctx, target, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<VulnerablePower>(ctx, target, DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(13);
        DynamicVars["WeakPower"].UpgradeValueBy(2);
        DynamicVars["VulnerablePower"].UpgradeValueBy(2);
    }
}

/// <summary>百万吨拳击: deal 9(12), or 37(50) when it is exactly the 4th card Kirby plays this turn.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class MegatonPunch() : KirbyCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    private const int TargetCount = 4;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(9, ValueProp.Move), new DynamicVar("MegatonDamage", 37)];

    // The play being resolved is already in the history, so the 4th card sees a count of 4.
    private int CardsPlayedThisTurn => CombatManager.Instance.History.CardPlaysStarted
        .Count(e => e.HappenedThisTurn(CombatState) && e.CardPlay.Card.Owner == Owner);

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var damage = CardsPlayedThisTurn == TargetCount
            ? DynamicVars["MegatonDamage"].BaseValue
            : DynamicVars.Damage.BaseValue;
        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars["MegatonDamage"].UpgradeValueBy(13);
    }
}

/// <summary>
/// 炸弹接力 (multiplayer only): target an ally or an enemy. An ally gets a copy carrying 20(25) more damage and this one
/// is gone; an enemy takes the current damage, and the card goes to the discard pile back at 20(25). Holding it at the
/// end of your turn costs you 20(25) HP. The holder shows <see cref="BombHolderPower" />.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class BombRally() : KirbyCard(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    private const int BaseDamage = 20;
    private const int UpgradeDamage = 5;

    /// <summary>Any living creature but the card's owner.</summary>
    private static readonly TargetType AnyoneElse = CustomTargetType.RegisterSingleTargetType(Entry.ModId, "anyone_else",
        (creature, player) => creature is { IsAlive: true, IsPet: false } && creature != player.Creature);

    public override TargetType TargetType => AnyoneElse;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override bool HasTurnEndInHandEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamage, ValueProp.Move), new DynamicVar("BombBase", BaseDamage)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<BombHolderPower>()];

    private int Step => DynamicVars["BombBase"].IntValue;

    // Passing hands the bomb over as a new card, so the played one leaves combat instead of being discarded.
    protected override PileType ResultPile(PileType basePile) =>
        CurrentTarget is { IsPlayer: true } ? PileType.None : basePile;

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target.Player is { } ally)
        {
            var bomb = KirbyMouth.CreateCard<BombRally>(ally, IsUpgraded);
            bomb.DynamicVars.Damage.BaseValue = DynamicVars.Damage.BaseValue + Step;
            await CardPileCmd.AddGeneratedCardToCombat(bomb, PileType.Hand, Owner);
            return;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);
        DynamicVars.Damage.BaseValue = Step;
        await RefreshHolder(Owner);
    }

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
#if STS2_V113
        await CreatureCmd.Damage(choiceContext, Owner.Creature, Step, ValueProp.Unpowered | ValueProp.Move, this, null);
#else
        await CreatureCmd.Damage(choiceContext, Owner.Creature, Step, ValueProp.Unpowered | ValueProp.Move, this);
#endif
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card == this && (oldPileType == PileType.Hand || Pile?.Type == PileType.Hand))
            await RefreshHolder(Owner);
    }

    /// <summary>Sets <see cref="BombHolderPower" /> to the total damage of the Bomb Rally cards in the player's hand.</summary>
    private static async Task RefreshHolder(Player player)
    {
        var total = PileType.Hand.GetPile(player).Cards.OfType<BombRally>().Sum(bomb => bomb.DynamicVars.Damage.IntValue);
        var holder = player.Creature.GetPower<BombHolderPower>();
        var current = holder?.Amount ?? 0;
        if (total == current)
            return;

        var ctx = new BlockingPlayerChoiceContext();
        if (total == 0)
            await PowerCmd.Remove(holder);
        else if (holder == null)
            await PowerCmd.Apply<BombHolderPower>(ctx, player.Creature, total, player.Creature, null, silent: true);
        else
            await PowerCmd.ModifyAmount(ctx, holder, total - current, player.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamage);
        DynamicVars["BombBase"].UpgradeValueBy(UpgradeDamage);
    }
}
