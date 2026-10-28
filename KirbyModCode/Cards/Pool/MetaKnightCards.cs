using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.HoverTips;
using KirbyMod.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace KirbyMod.Cards;

/// <summary>帮助 (Helping Hand, was 魅塔骑士的帮助): you or an ally gains 5(7) Vigor.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class MetaKnightAid() : KirbyCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyPlayer)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VigorPower>(5)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        // Single player has no target selection for AnyPlayer.
        var target = cardPlay.Target ?? Owner.Creature;
        await PowerCmd.Apply<VigorPower>(ctx, target, DynamicVars[nameof(VigorPower)].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars[nameof(VigorPower)].UpgradeValueBy(2);
}

/// <summary>魅塔骑士的复仇: deal 25(37). Sword: costs 0 (done in <see cref="Powers.SwordPower" />). Counts as a Sword card.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class MetaKnightRevenge() : KirbyCard(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(25, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_big_slash").Execute(ctx);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(12);
}

/// <summary>
/// 魅塔梦魇 (skill, Exhaust): your Sword cards cost 0 this turn, gain Sword and add a Mach Tornado to the discard pile. Upgrade: cost 2.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class MetaNightmare() : KirbyCard(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<SwordPower>(), HoverTipFactory.FromCard<MachTornado>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        // First, so the Sword card handed out by gaining Sword is free too.
        if (!Owner.Creature.HasPower<MetaNightmarePower>())
            await PowerCmd.Apply<MetaNightmarePower>(ctx, Owner.Creature, 1, Owner.Creature, this);

        await CopyAbilities.Gain(ctx, Owner, CopyAbilityKind.Sword, upgraded: false, this);
        await CardPileCmd.AddGeneratedCardToCombat(KirbyMouth.CreateCard<MachTornado>(Owner, false), PileType.Discard, Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
