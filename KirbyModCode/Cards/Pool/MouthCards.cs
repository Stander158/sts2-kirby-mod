using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace KirbyMod.Cards;

/// <summary>无能力吸入 (class name kept from 古伊吸入): Inhale that is not exhausted and only gives Mouthful, no ability in the mouth. Upgrade adds Retain and upgraded mouth cards.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class GooeyInhale() : KirbyCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy), IInhaleCard
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => Inhale.GetInhaleHoverTips(IsUpgraded);

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await KirbyMouth.StartInhale(ctx, Owner, cardPlay.Target!, this);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

/// <summary>泡泡储存 (power, Innate): Swallow no longer clears Mouthful. Upgrade: cost 0.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class BubbleStorage() : KirbyCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<Swallow>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BubbleStoragePower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>超级能力 (power): double the benefits of the next 1(2) copy abilities you gain.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class SuperAbility() : KirbyCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<SuperAbilityPower>(1)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<SuperAbilityPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await PowerCmd.Apply<SuperAbilityPower>(ctx, Owner.Creature, DynamicVars[nameof(SuperAbilityPower)].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars[nameof(SuperAbilityPower)].UpgradeValueBy(1);
}

/// <summary>大乱斗卡比 (power): copy abilities can coexist. Upgrade: cost 1.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class SmashBrosKirby() : KirbyCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<SmashBrosPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await PowerCmd.Apply<SmashBrosPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>星之齿轮 (power): Spit Star deals +50%(100%) damage; a Spit Star of 20+ damage also hits every other enemy.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class StarBulletBoost() : KirbyCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<StarBulletPower>(50), new DynamicVar("SplashThreshold", StarBulletPower.SplashThreshold)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<SpitStar>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await PowerCmd.Apply<StarBulletPower>(ctx, Owner.Creature, DynamicVars[nameof(StarBulletPower)].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars[nameof(StarBulletPower)].UpgradeValueBy(50);
}
