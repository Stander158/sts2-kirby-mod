using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.CardTargeting;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Cards;

/// <summary>Doctor cards: "Doctor: Retain, Replay 1" for the two that have it.</summary>
public abstract class DoctorReplayCard(int cost, CardType type, CardRarity rarity)
    : KirbyCard(cost, type, rarity, CustomTargetType.Anyone)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Doctor];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount) =>
        card == this && SynergyActive ? playCount + 1 : playCount;
}

/// <summary>制药: an enemy gets 2(3) Vulnerable or 2(3) Weak (your choice); you or an ally loses all Vulnerable and Weak. Exhaust. Doctor: Retain, Replay 1.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class MakeMedicine() : DoctorReplayCard(1, CardType.Skill, CardRarity.Uncommon)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<VulnerablePower>(2), new PowerVar<WeakPower>(2)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<VulnerablePower>(), HoverTipFactory.FromPower<WeakPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var target = cardPlay.Target ?? Owner.Creature;
        if (target.IsPlayer)
        {
            await PowerCmd.Remove(target.GetPower<VulnerablePower>());
            await PowerCmd.Remove(target.GetPower<WeakPower>());
            return;
        }

        var options = new CardModel[]
        {
            KirbyMouth.CreateCard<MedicineVulnerable>(Owner, IsUpgraded),
            KirbyMouth.CreateCard<MedicineWeak>(Owner, IsUpgraded),
        };
        var choice = await CardSelectCmd.FromChooseACardScreen(ctx, options, Owner);
        if (choice is MedicineWeak)
            await PowerCmd.Apply<WeakPower>(ctx, target, DynamicVars[nameof(WeakPower)].BaseValue, Owner.Creature, this);
        else
            await PowerCmd.Apply<VulnerablePower>(ctx, target, DynamicVars[nameof(VulnerablePower)].BaseValue,
                Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[nameof(VulnerablePower)].UpgradeValueBy(1);
        DynamicVars[nameof(WeakPower)].UpgradeValueBy(1);
    }
}

/// <summary>Display-only option for <see cref="MakeMedicine" />: Vulnerable.</summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class MedicineVulnerable() : ModCardTemplate(1, CardType.Skill, CardRarity.Token, TargetType.None, false)
{
    public override CardAssetProfile AssetProfile => KirbyArt.ForCard(typeof(MakeMedicine));

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VulnerablePower>(2)];

    protected override void OnUpgrade() => DynamicVars[nameof(VulnerablePower)].UpgradeValueBy(1);
}

/// <summary>Display-only option for <see cref="MakeMedicine" />: Weak.</summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class MedicineWeak() : ModCardTemplate(1, CardType.Skill, CardRarity.Token, TargetType.None, false)
{
    public override CardAssetProfile AssetProfile => KirbyArt.ForCard(typeof(MakeMedicine));

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<WeakPower>(2)];

    protected override void OnUpgrade() => DynamicVars[nameof(WeakPower)].UpgradeValueBy(1);
}

/// <summary>投掷药丸: an enemy takes 7(9) damage; you or an ally heals 3(4). Exhaust. Doctor: Retain, Replay 1.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class PillThrow() : DoctorReplayCard(1, CardType.Attack, CardRarity.Common)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7, ValueProp.Move), new HealVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var target = cardPlay.Target ?? Owner.Creature;
        if (target.IsPlayer)
        {
            VfxCmd.PlayOnCreatureCenter(target, "vfx/vfx_cross_heal");
            await CreatureCmd.Heal(target, DynamicVars.Heal.BaseValue);
            return;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target)
            .WithHitFx("vfx/vfx_attack_blunt").Execute(ctx);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars.Heal.UpgradeValueBy(1);
    }
}

/// <summary>
/// 实验 (3-cost power): you become the Attending Physician, and the chosen creature (an enemy, an ally or yourself)
/// becomes your Test Subject. Doctor: costs 1 less (in <see cref="DoctorPower" />), Retain. Upgrade: cost 2.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class Experiment() : KirbyCard(2, CardType.Power, CardRarity.Rare, CustomTargetType.Anyone)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Doctor];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            HoverTipFactory.FromPower<AttendingPhysicianPower>(), HoverTipFactory.FromPower<TestSubjectPower>(),
            HoverTipFactory.FromCard<Dazed>(),
        ];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var target = cardPlay.Target ?? Owner.Creature;
        if (!Owner.Creature.HasPower<AttendingPhysicianPower>())
            await PowerCmd.Apply<AttendingPhysicianPower>(ctx, Owner.Creature, 1, Owner.Creature, this);

        // A creature has one physician: a new Experiment on it replaces the old one.
        await PowerCmd.Remove(target.GetPower<TestSubjectPower>());
        var subject = (TestSubjectPower)ModelDb.Power<TestSubjectPower>().ToMutable();
        subject.Physician = Owner;
        await PowerCmd.Apply(ctx, subject, target, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
