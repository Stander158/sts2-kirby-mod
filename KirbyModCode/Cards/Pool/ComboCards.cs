using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace KirbyMod.Cards;

/// <summary>
/// 深度睡眠 (Deep Sleep): Rare power, 0. Gain 1 Slumber and add a Sleep Ability card to hand. Upgraded: the card is
/// Sleep Ability+ and you also gain 3 Ability Conservation (each one keeps Sleep through one unblocked hit).
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class DeepSleep() : KirbyCard(0, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    private const int UpgradedConservation = 3;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => IsUpgraded
        ?
        [
            HoverTipFactory.FromPower<KirbySlumberPower>(), HoverTipFactory.FromCard<SleepAbility>(true),
            HoverTipFactory.FromPower<AbilityConservationPower>(),
        ]
        : [HoverTipFactory.FromPower<KirbySlumberPower>(), HoverTipFactory.FromCard<SleepAbility>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await PowerCmd.Apply<KirbySlumberPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
        if (IsUpgraded)
            await PowerCmd.Apply<AbilityConservationPower>(ctx, Owner.Creature, UpgradedConservation, Owner.Creature,
                this);
        await CardPileCmd.AddGeneratedCardToCombat(KirbyMouth.CreateCard<SleepAbility>(Owner, IsUpgraded),
            PileType.Hand, Owner);
    }
}

/// <summary>
/// 捕捉光束 (Capture Beam): Common skill, 1(0). This turn, every 2 cards played after it give the enemy 2 Clock. Beam:
/// Retain; at the end of turn it takes damage equal to the Clock it gained this turn.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class CaptureBeam() : KirbyCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Beam];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Clock", 2)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<ClockPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        var synergy = SynergyActive;
        KirbyFx.PlaySound(KirbyFx.BeamSound);
        var power = await PowerCmd.Apply<CaptureBeamPower>(ctx, target, DynamicVars["Clock"].BaseValue, Owner.Creature,
            this);
        power?.Track(this, synergy);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>
/// 光束机关枪 (Beam Blast): Uncommon skill, 1. Apply 4(6) Clock; if that sends it into Stasis, all players gain 2
/// energy and draw 2. Beam: Retain, apply 1 Vulnerable.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class BeamGatling() : KirbyCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Beam];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Clock", 4), new EnergyVar(2), new CardsVar(2), new PowerVar<VulnerablePower>(1)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<ClockPower>(), HoverTipFactory.FromPower<StasisPower>(),
        HoverTipFactory.FromPower<VulnerablePower>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        var synergy = SynergyActive;
        var immunity = target.GetPowerAmount<TimeImmunityPower>();

        KirbyFx.PlaySound(KirbyFx.BeamSound);
        await PowerCmd.Apply<ClockPower>(ctx, target, DynamicVars["Clock"].BaseValue, Owner.Creature, this);

        // Clock going off gives 1 Timing Immunity.
        if (target.GetPowerAmount<TimeImmunityPower>() > immunity)
            foreach (var player in CombatState!.Players.Where(player => player.Creature.IsAlive).ToList())
            {
                await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, player);
                await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, player);
            }

        if (synergy && target.IsAlive)
            await PowerCmd.Apply<VulnerablePower>(ctx, target, DynamicVars[nameof(VulnerablePower)].BaseValue,
                Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Clock"].UpgradeValueBy(2);
}

/// <summary>火箭起步 (Rocket Start): Rare power, 2(1). Double your Dexterity. Wheel: Retain, gain 1 Block.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class RocketStart() : KirbyCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Wheel];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DexterityPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var synergy = SynergyActive;
        var kirby = Owner.Creature;
        var dex = kirby.GetPowerAmount<DexterityPower>();
        if (dex != 0)
            await PowerCmd.Apply<DexterityPower>(ctx, kirby, dex, kirby, this);
        if (synergy)
            await CreatureCmd.GainBlock(kirby, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>
/// 篝火 (Bonfire): Common skill, 1. Every Curse and Status (but Burns) in hand, draw and discard pile becomes an Ethereal
/// Burn. Fire: Retain; this combat, Burns played deal 5(8) more damage.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class Bonfire() : KirbyCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Fire];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("BurnBonus", 5)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromCard<Burn>(), HoverTipFactory.FromPower(CopyAbilities.CanonicalPower(CopyAbilityKind.Fire))];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var synergy = SynergyActive;
        var fire = CopyAbilities.Has(Owner, [CopyAbilityKind.Fire]);

        var cards = KirbyMouth.LivePiles
            .SelectMany(pile => pile.GetPile(Owner).Cards)
            .Where(card => card.Type is CardType.Curse or CardType.Status && card is not Burn && card.IsTransformable)
            .ToList();
        foreach (var card in cards)
        {
            var burn = KirbyMouth.CreateCard<Burn>(Owner, upgraded: false);
            burn.AddKeyword(CardKeyword.Ethereal);
            await CardCmd.Transform(card, burn);
            if (fire)
                burn.RemoveKeyword(CardKeyword.Unplayable);
        }

        if (synergy)
            await PowerCmd.Apply<BonfirePower>(ctx, Owner.Creature, DynamicVars["BurnBonus"].BaseValue, Owner.Creature,
                this);
    }

    protected override void OnUpgrade() => DynamicVars["BurnBonus"].UpgradeValueBy(3);
}

/// <summary>火焰缠身 (Flame Mantle): Uncommon power, 2(1). Whenever a Status enters your discard pile, draw 1. Fire: Retain.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class FlameMantle() : KirbyCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Fire];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay) =>
        await PowerCmd.Apply<FlameMantlePower>(ctx, Owner.Creature, DynamicVars.Cards.BaseValue, Owner.Creature, this);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>
/// 药瓶 (Medicine Bottle): Uncommon skill, 1, Exhaust. Apply 6(9) Doom, 2(4) Poison, 1 Frail; it loses 1(2) Strength
/// this turn. Gain Block equal to the number of different powers on it. Doctor: Retain, Replay 1.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class MedicineBottle() : KirbyCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Doctor];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<DoomPower>(6), new PowerVar<PoisonPower>(2), new PowerVar<FrailPower>(1),
        new DynamicVar("StrengthLoss", 1),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<DoomPower>(), HoverTipFactory.FromPower<PoisonPower>(),
        HoverTipFactory.FromPower<FrailPower>(), HoverTipFactory.FromPower<StrengthPower>(),
    ];

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount) =>
        card == this && SynergyActive ? playCount + 1 : playCount;

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        var kirby = Owner.Creature;

        await PowerCmd.Apply<DoomPower>(ctx, target, DynamicVars[nameof(DoomPower)].BaseValue, kirby, this);
        await PowerCmd.Apply<PoisonPower>(ctx, target, DynamicVars[nameof(PoisonPower)].BaseValue, kirby, this);
        await PowerCmd.Apply<FrailPower>(ctx, target, DynamicVars[nameof(FrailPower)].BaseValue, kirby, this);

        // The Strength comes back at the end of its turn, as much as was actually lost (Artifact may block it).
        var before = target.GetPowerAmount<StrengthPower>();
        await PowerCmd.Apply<StrengthPower>(ctx, target, -DynamicVars["StrengthLoss"].BaseValue, kirby, this);
        var lost = before - target.GetPowerAmount<StrengthPower>();
        if (lost > 0)
            await PowerCmd.Apply<MedicineBottlePower>(ctx, target, lost, kirby, this);

        var kinds = target.Powers.Count(power => power.IsVisible);
        if (kinds > 0)
            await CreatureCmd.GainBlock(kirby, kinds, ValueProp.Unpowered, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[nameof(DoomPower)].UpgradeValueBy(3);
        DynamicVars[nameof(PoisonPower)].UpgradeValueBy(2);
        DynamicVars["StrengthLoss"].UpgradeValueBy(1);
    }
}

/// <summary>
/// 巨石 (Boulder): Rare skill, 2. Lose 5 Dexterity, gain 4(5) Buffer. Stone: Retain; at the end of your turn, if it is in
/// your hand, gain 1 Buffer.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class Boulder() : KirbyCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Stone];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("DexLoss", 5), new PowerVar<BufferPower>(4)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<DexterityPower>(), HoverTipFactory.FromPower<BufferPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DexterityPower>(ctx, Owner.Creature, -DynamicVars["DexLoss"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<BufferPower>(ctx, Owner.Creature, DynamicVars[nameof(BufferPower)].BaseValue, Owner.Creature,
            this);
    }

    // Not HasTurnEndInHandEffect: the game would move the card through the play pile to the discard pile, losing Retain.
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || Pile?.Type != PileType.Hand || !SynergyActive || !Owner.Creature.IsAlive)
            return;

        await PowerCmd.Apply<BufferPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars[nameof(BufferPower)].UpgradeValueBy(1);
}
