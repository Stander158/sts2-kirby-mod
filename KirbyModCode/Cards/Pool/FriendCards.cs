using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace KirbyMod.Cards;

/// <summary>
/// 朋友平台 (multiplayer only): every player picks one of Friend Train, Friend Bridge and Friend Circle (upgraded when
/// this is) into their hand, all at the same time. Exhaust.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class FriendPlatform() : KirbyCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<FriendTrain>(IsUpgraded),
        HoverTipFactory.FromCard<FriendBridge>(IsUpgraded),
        HoverTipFactory.FromCard<FriendCircle>(IsUpgraded),
    ];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var players = Owner.Creature.CombatState!.PlayerCreatures
            .Where(creature => creature.IsAlive && creature.Player != null)
            .Select(creature => creature.Player!);
        // Like the Knowledge Demon's curse pick: everyone chooses at once, each on their own screen.
        await Task.WhenAll(players.Select(player => Choose(player == Owner ? ctx : new BlockingPlayerChoiceContext(), player)));
    }

    private async Task Choose(PlayerChoiceContext ctx, Player player)
    {
        var options = new CardModel[]
        {
            KirbyMouth.CreateCard<FriendTrain>(player, IsUpgraded),
            KirbyMouth.CreateCard<FriendBridge>(player, IsUpgraded),
            KirbyMouth.CreateCard<FriendCircle>(player, IsUpgraded),
        };
        var choice = await CardSelectCmd.FromChooseACardScreen(ctx, options, player);
        if (choice != null)
            await CardPileCmd.AddGeneratedCardToCombat(choice, PileType.Hand, Owner);
    }
}

/// <summary>Base for the Friend Platform cards: multiplayer tokens that are Ethereal and Exhaust.</summary>
public abstract class FriendCard(int cost, CardType type, TargetType target) : KirbyCard(cost, type, CardRarity.Token, target)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Exhaust];
}

/// <summary>朋友火车: deal 10(12); this turn Friend Train hits on that enemy are doubled, stacking like Hang.</summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class FriendTrain() : FriendCard(1, CardType.Attack, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);

        // Hang's rule: the multiplier doubles with every hit (x2, x4, x8...).
        var current = target.GetPowerAmount<FriendTrainPower>();
        await PowerCmd.Apply<FriendTrainPower>(ctx, target, Math.Max(2, current), Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}

/// <summary>朋友桥梁: every other player gains 4(5) Plating.</summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class FriendBridge() : FriendCard(1, CardType.Skill, TargetType.AllAllies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PlatingPower>(4)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<PlatingPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var others = Owner.Creature.CombatState!.PlayerCreatures
            .Where(creature => creature.IsAlive && creature != Owner.Creature);
        await PowerCmd.Apply<PlatingPower>(ctx, others, DynamicVars["PlatingPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["PlatingPower"].UpgradeValueBy(1);
}

/// <summary>朋友圆圈: an ally gains 1 energy and draws 1(2).</summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class FriendCircle() : FriendCard(1, CardType.Skill, TargetType.AnyAlly)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1), new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var ally = cardPlay.Target!.Player;
        if (ally == null)
            return;

        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, ally);
        await CardPileCmd.Draw(ctx, DynamicVars.Cards.IntValue, ally);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}
