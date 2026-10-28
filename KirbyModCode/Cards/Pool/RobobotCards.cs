using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace KirbyMod.Cards;

/// <summary>萝卜机甲 (power): gain 3(4) Ability Conservation and Robobot; every Inhale you have becomes Scan.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class Robobot() : KirbyCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    private static readonly PileType[] InhalePiles = [.. KirbyMouth.LivePiles, PileType.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<AbilityConservationPower>(3), new DynamicVar("RobobotBonus", RobobotPower.Bonus)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AbilityConservationPower>(),
        HoverTipFactory.FromPower<RobobotPower>(),
        HoverTipFactory.FromCard<Scan>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var kirby = Owner.Creature;
        await PowerCmd.Apply<AbilityConservationPower>(ctx, kirby,
            DynamicVars[nameof(AbilityConservationPower)].BaseValue, kirby, this);
        if (!kirby.HasPower<RobobotPower>())
            await PowerCmd.Apply<RobobotPower>(ctx, kirby, 1, kirby, this);

        var inhales = InhalePiles.SelectMany(pile => pile.GetPile(Owner).Cards).OfType<Inhale>().ToList();
        foreach (var inhale in inhales)
            await CardCmd.Transform(inhale, KirbyMouth.CreateCard<Scan>(Owner, inhale.IsUpgraded));
    }

    protected override void OnUpgrade() => DynamicVars[nameof(AbilityConservationPower)].UpgradeValueBy(1);
}

/// <summary>无敌棒棒糖: gain 1(2) Invincibility. Exhaust.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class InvincibleLollipop() : KirbyCard(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<InvincibilityPower>(1)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<InvincibilityPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await PowerCmd.Apply<InvincibilityPower>(ctx, Owner.Creature, DynamicVars[nameof(InvincibilityPower)].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars[nameof(InvincibilityPower)].UpgradeValueBy(1);
}

/// <summary>躲避: gain 4(5) Block twice and 1(2) Ability Conservation.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class Dodge() : KirbyCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(4, ValueProp.Move), new RepeatVar(2), new PowerVar<AbilityConservationPower>(1)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<AbilityConservationPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        for (var i = 0; i < DynamicVars.Repeat.IntValue; i++)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<AbilityConservationPower>(ctx, Owner.Creature,
            DynamicVars[nameof(AbilityConservationPower)].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1);
        DynamicVars[nameof(AbilityConservationPower)].UpgradeValueBy(1);
    }
}
