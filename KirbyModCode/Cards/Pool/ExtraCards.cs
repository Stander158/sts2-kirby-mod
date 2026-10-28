using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace KirbyMod.Cards;

/// <summary>霜冻: turn up to 1(2) cards in hand into Ice Blocks. Ice: Retain, and this turn Ice Blocks deal +4(6) damage and give +3(5) Block.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class Frost() : KirbyCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Ice];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(1), new DynamicVar("IceDamageBonus", 4), new DynamicVar("IceBlockBonus", 3)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<IceBlock>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var candidates = PileType.Hand.GetPile(Owner).Cards.Count(card => card is not IceBlock);
        if (candidates > 0)
        {
            var chosen = await CardSelectCmd.FromHand(ctx, Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 0, Math.Min(candidates, DynamicVars.Cards.IntValue)),
                card => card is not IceBlock, this);
            foreach (var card in chosen.ToList())
                await CardCmd.Transform(card, KirbyMouth.CreateCard<IceBlock>(Owner, false));
        }

        if (!SynergyActive)
            return;

        await PowerCmd.Apply<FrostDamagePower>(ctx, Owner.Creature, DynamicVars["IceDamageBonus"].BaseValue,
            Owner.Creature, this);
        await PowerCmd.Apply<FrostBlockPower>(ctx, Owner.Creature, DynamicVars["IceBlockBonus"].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
        DynamicVars["IceDamageBonus"].UpgradeValueBy(2);
        DynamicVars["IceBlockBonus"].UpgradeValueBy(2);
    }
}

/// <summary>火球 (skill): deal 18(26), add 2 Burns to hand. Fire: Retain.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class KirbyFireball() : KirbyCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Fire];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(18, ValueProp.Move), new CardsVar(2)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<Burn>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_fire_burst").Execute(ctx);

        for (var i = 0; i < DynamicVars.Cards.IntValue; i++)
            await CardPileCmd.AddGeneratedCardToCombat(KirbyMouth.CreateCard<Burn>(Owner, false), PileType.Hand, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(8);
}

/// <summary>究极剑斩: Exhaust. Deal 14(20); Vigor affects this card 3(5) times. Sword: Retain, Replay 1.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class UltraSword() : KirbyCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Sword];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(14, ValueProp.Move), new DynamicVar("VigorTimes", 3)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<VigorPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_giant_horizontal_slash").Execute(ctx);
    }

    // Vigor itself adds its amount once; this adds the remaining (times - 1) copies.
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        if (cardSource != this || !IsMutable || dealer != Owner.Creature || !props.IsPoweredAttack())
            return 0m;
        return dealer.GetPowerAmount<VigorPower>() * (DynamicVars["VigorTimes"].BaseValue - 1);
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount) =>
        card == this && SynergyActive ? playCount + 1 : playCount;

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6);
        DynamicVars["VigorTimes"].UpgradeValueBy(2);
    }
}

/// <summary>究极火龙 (3 cost): Exhaust. Gain the Fire copy ability, then deal 5(7) damage for each Burn you have. Fire: Retain; afterwards move Burns from the discard pile to hand until it is full.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class DragonStorm() : KirbyCard(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Fire];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0),
        new ExtraDamageVar(5),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier((card, _) => card.IsMutable ? BurnCount(card.Owner) : 0),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<FirePower>(), HoverTipFactory.FromCard<Burn>()];

    private static int BurnCount(Player player) =>
        KirbyMouth.LivePiles.SelectMany(pile => pile.GetPile(player).Cards).Count(card => card is Burn);

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CopyAbilities.Gain(ctx, Owner, CopyAbilityKind.Fire, upgraded: false, this);

        await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this).Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_fire_burst").Execute(ctx);

        if (!SynergyActive)
            return;

        var hand = PileType.Hand.GetPile(Owner);
        foreach (var burn in PileType.Discard.GetPile(Owner).Cards.OfType<Burn>().ToList())
        {
            if (hand.Cards.Count >= CardPile.MaxCardsInHand)
                break;
            await CardPileCmd.Add(burn, PileType.Hand);
        }
    }

    protected override void OnUpgrade() => DynamicVars.ExtraDamage.UpgradeValueBy(2);
}

/// <summary>
/// 究极冰球 (3 cost, Exhaust): gain the Ice copy ability, exhaust every other card in your hand, then fill it with Ice
/// Blocks (upgraded when this is). Ice: Retain, and for the rest of combat Ice Blocks deal damage AND give Block.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class SnowBowl() : KirbyCard(3, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Ice];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<IcePower>(), HoverTipFactory.FromCard<IceBlock>(IsUpgraded)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CopyAbilities.Gain(ctx, Owner, CopyAbilityKind.Ice, upgraded: false, this);

        var hand = PileType.Hand.GetPile(Owner);
        foreach (var card in hand.Cards.Where(card => card != this).ToList())
            await CardCmd.Exhaust(ctx, card);

        while (hand.Cards.Count < CardPile.MaxCardsInHand)
            await CardPileCmd.AddGeneratedCardToCombat(KirbyMouth.CreateCard<IceBlock>(Owner, IsUpgraded),
                PileType.Hand, Owner);

        if (SynergyActive)
            await PowerCmd.Apply<SnowBowlPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }
}

/// <summary>冰壶 (0 cost): you or an ally loses all Block and gains 1 Buffer per 15(10) Block lost. Exhaust. Stone: Retain, and it is not exhausted.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class CurlingStone() : KirbyCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyPlayer)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Stone];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("BlockPerBuffer", 15)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<BufferPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        // Single player has no target selection for AnyPlayer.
        var target = cardPlay.Target ?? Owner.Creature;
        var block = target.Block;
        if (block <= 0)
            return;

        await CreatureCmd.LoseBlock(target, block);
        var buffers = block / DynamicVars["BlockPerBuffer"].IntValue;
        if (buffers > 0)
            await PowerCmd.Apply<BufferPower>(ctx, target, buffers, Owner.Creature, this);
    }

    // Exhaust is written in the text instead of being a keyword, since Stone turns it off.
    protected override PileType GetResultPileTypeForCardPlay() =>
        SynergyActive ? base.GetResultPileTypeForCardPlay() : PileType.Exhaust;

    protected override void OnUpgrade() => DynamicVars["BlockPerBuffer"].UpgradeValueBy(-5);
}

/// <summary>粉红恶魔: apply 1(2) Vulnerable and 1 Weak. If you have a copy ability, draw 1(2).</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class PinkDemon() : KirbyCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<VulnerablePower>(1), new PowerVar<WeakPower>(1), new CardsVar(1)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<VulnerablePower>(), HoverTipFactory.FromPower<WeakPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        await PowerCmd.Apply<VulnerablePower>(ctx, target, DynamicVars[nameof(VulnerablePower)].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<WeakPower>(ctx, target, DynamicVars[nameof(WeakPower)].BaseValue, Owner.Creature, this);
        if (CopyAbilities.HasAny(Owner))
            await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[nameof(VulnerablePower)].UpgradeValueBy(1);
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
