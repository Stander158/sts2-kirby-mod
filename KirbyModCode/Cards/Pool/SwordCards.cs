using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace KirbyMod.Cards;

/// <summary>劈斩: deal 9(12). Sword: Retain, +4(6) damage.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class Slash() : KirbyCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Sword];

    protected override IEnumerable<DynamicVar> CanonicalVars => SynergyDamage(9, 4);

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this).Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash").Execute(ctx);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(3);
        DynamicVars.ExtraDamage.UpgradeValueBy(2);
    }
}

/// <summary>回旋斩: deal 3 damage 3(4) times. Sword: Retain, +1 damage per hit.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class SpinSlash() : KirbyCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Sword];

    protected override IEnumerable<DynamicVar> CanonicalVars => [.. SynergyDamage(3, 1), new RepeatVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).WithHitCount(DynamicVars.Repeat.IntValue).FromCard(this)
            .Targeting(cardPlay.Target!).WithHitFx("vfx/vfx_attack_slash").Execute(ctx);
    }

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1);
}

/// <summary>升龙斩: deal 8(10), add a Descending Slash(+) to hand; Descending Slashes cost 0 this turn. Sword/Boomerang: Retain, +4(5).</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class RisingSlash() : KirbyCard(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Sword, CopyAbilityKind.Boomerang];

    protected override IEnumerable<DynamicVar> CanonicalVars => SynergyDamage(8, 4);

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<DescendingSlash>(IsUpgraded)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this).Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash").Execute(ctx);

        await CardPileCmd.AddGeneratedCardToCombat(KirbyMouth.CreateCard<DescendingSlash>(Owner, IsUpgraded),
            PileType.Hand, Owner);

        // "If Rising Slash was played earlier this turn, Descending Slash costs 0."
        foreach (var card in KirbyMouth.LivePiles.SelectMany(pile => pile.GetPile(Owner).Cards).OfType<DescendingSlash>())
            card.SetToFreeThisTurn();
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(2);
        DynamicVars.ExtraDamage.UpgradeValueBy(1);
    }
}

/// <summary>降龙斩 (made by Rising Slash, 1 cost): deal 8(10). Sword/Boomerang: Retain, +4(5).</summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class DescendingSlash() : KirbyCard(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Sword, CopyAbilityKind.Boomerang];

    protected override IEnumerable<DynamicVar> CanonicalVars => SynergyDamage(8, 4);

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this).Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash").Execute(ctx);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(2);
        DynamicVars.ExtraDamage.UpgradeValueBy(1);
    }
}

/// <summary>马赫龙卷风: 0 cost, deal 3(4). Sword: Retain, +2(3). Whenever you play another Sword attack, return it from the discard pile to hand.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class MachTornado() : KirbyCard(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Sword];

    protected override IEnumerable<DynamicVar> CanonicalVars => SynergyDamage(3, 2);

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this).Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash").Execute(ctx);
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner && IsOtherSwordAttack(cardPlay.Card) && Pile?.Type == PileType.Discard)
            await CardPileCmd.Add(this, PileType.Hand);
    }

    // Sword attacks: attacks that get a Sword bonus, plus Meta Knight's Revenge. Mach Tornadoes don't count.
    private static bool IsOtherSwordAttack(CardModel card) =>
        card.Type == CardType.Attack && card is not MachTornado
        && (card is MetaKnightRevenge
            || (card is IAbilitySynergyCard synergy && synergy.SynergyAbilities.Contains(CopyAbilityKind.Sword)));

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(1);
        DynamicVars.ExtraDamage.UpgradeValueBy(1);
    }
}
