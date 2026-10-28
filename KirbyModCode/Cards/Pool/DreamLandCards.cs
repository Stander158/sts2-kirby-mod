using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using VanillaCards = MegaCrit.Sts2.Core.Models.Cards;

namespace KirbyMod.Cards;

/// <summary>矿洞大作战: gain 13(18) Block. If you have a copy ability, drop it (Inhale returns to hand) and gain 1 energy.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class MineBattle() : KirbyCard(2, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(13, ValueProp.Move), new EnergyVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (!CopyAbilities.HasAny(Owner))
            return;

        await KirbyMouth.DropCopyAbility(ctx, Owner);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(5);
}

/// <summary>春风: with a copy ability, gain 2(3) energy; without one, draw 2 cards instead.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class SpringBreeze() : KirbyCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2), new EnergyVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        if (CopyAbilities.HasAny(Owner))
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        else
            await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1);
}

/// <summary>铁翼: deal 7(9). Every Strike in your hand becomes a Peck (upgraded: Peck+) for this combat.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class SteelWing() : KirbyCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<VanillaCards.Peck>(IsUpgraded)];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash").Execute(ctx);

        var strikes = PileType.Hand.GetPile(Owner).Cards.Where(card => card.Tags.Contains(CardTag.Strike)).ToList();
        foreach (var strike in strikes)
            await CardCmd.Transform(strike, KirbyMouth.CreateCard<VanillaCards.Peck>(Owner, IsUpgraded));
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}

/// <summary>梦之泉: shuffle 1(2) Wish into your draw pile.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class FountainOfDreams() : KirbyCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<VanillaCards.Wish>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        for (var i = 0; i < DynamicVars.Cards.IntValue; i++)
        {
            await CardPileCmd.AddGeneratedCardToCombat(KirbyMouth.CreateCard<VanillaCards.Wish>(Owner, false),
                PileType.Draw, Owner, CardPilePosition.Random);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

/// <summary>镜之大迷宫: an enemy intending to attack deals 0 with its attacks this turn and gains that much Block instead. Upgrade: cost 1.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class MirrorLabyrinth() : KirbyCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target.Monster?.NextMove.Intents.Any(intent => intent is AttackIntent) == true)
            await PowerCmd.Apply<MirroredPower>(ctx, target, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
