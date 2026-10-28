using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Cards;

/// <summary>
/// 吐星星: deal damage equal to Mouthful (+50%/100% per Star Bullet Boost), empty the mouth, and shuffle Inhale back
/// into the draw pile. With Star Bullet Boost, a Spit Star of 20+ damage hits every enemy.
/// </summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class SpitStar() : ModCardTemplate(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
{
    public override CardAssetProfile AssetProfile => KirbyArt.ForCard(GetType());

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0m),
        new ExtraDamageVar(1m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(StarDamage),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<MouthfulPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var attack = DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this).WithHitFx("vfx/vfx_attack_blunt");
        var splash = Owner.Creature.HasPower<StarBulletPower>()
                     && DynamicVars.CalculatedDamage.Calculate(cardPlay.Target) >= StarBulletPower.SplashThreshold;
        await (splash ? attack.TargetingAllOpponents(CombatState!) : attack.Targeting(cardPlay.Target!)).Execute(ctx);

        // Star Rod: spitting keeps the Mouthful.
        await KirbyMouth.ClearMouth(Owner.Creature, keepMouthful: Owner.Creature.HasPower<StarRodPower>());

        var inhale = KirbyMouth.FindExhaustedInhale(Owner);
        if (inhale != null)
            await CardPileCmd.Add(inhale, PileType.Draw, CardPilePosition.Random);

        await KirbyMouth.ExhaustOtherMouthCards(ctx, Owner, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    private static decimal StarDamage(CardModel card, Creature? _)
    {
        var kirby = card.Owner.Creature;
        var bonusPercent = kirby.GetPower<StarBulletPower>()?.Amount ?? 0;
        return Math.Floor(KirbyMouth.MouthfulOf(kirby) * (100m + bonusPercent) / 100m);
    }
}
