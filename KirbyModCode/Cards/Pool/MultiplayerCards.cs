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

/// <summary>喂食 (multiplayer only): spend up to 20(30) Mouthful to heal an ally half that much (rounded down). Exhaust.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class Feed() : KirbyCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyAlly)
{
    private const int MaxSpent = 20;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(MaxSpent)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<MouthfulPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var kirby = Owner.Creature;
        var mouthful = kirby.GetPower<MouthfulPower>();
        var amount = Math.Min(mouthful?.Amount ?? 0, DynamicVars.Heal.IntValue);
        if (mouthful == null || amount <= 0)
            return;

        // Only the Mouthful actually fed is spent.
        await PowerCmd.ModifyAmount(ctx, mouthful, -amount, kirby, this);
        var heal = Math.Floor(amount / 2m);
        if (heal > 0)
            await CreatureCmd.Heal(cardPlay.Target!, heal);
    }

    protected override void OnUpgrade() => DynamicVars.Heal.UpgradeValueBy(10);
}

/// <summary>
/// 能力奉献 (multiplayer only): lose your copy ability (or abilities) and give an ally the matching ability card, which
/// costs 0 for the rest of combat.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class AbilityOffering() : KirbyCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var ally = cardPlay.Target!.Player;
        var held = CopyAbilities.Held(Owner).Select(power => (power.Kind, power.Upgraded)).ToList();
        if (ally == null || held.Count == 0 || await KirbyMouth.BlockedByVoid(ctx, Owner))
            return;

        await KirbyMouth.LoseCopyAbility(ctx, Owner);
        await CopyAbilities.AfterAbilityLost(Owner);

        foreach (var (kind, upgraded) in held)
        {
            var card = CopyAbilities.CreateAbilityCard(ally, kind);
            if (upgraded && card.IsUpgradable)
                CardCmd.Upgrade(card);
            card.SetToFreeThisCombat();
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
