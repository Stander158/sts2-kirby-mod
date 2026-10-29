using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace KirbyMod.Cards;

/// <summary>向银河许愿 (power): whenever you gain a copy ability, gain 1 energy. Upgrade: ability cards are no longer exhausted.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class GalaxyWish() : KirbyCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await PowerCmd.Apply<GalaxyWishPower>(ctx, Owner.Creature, DynamicVars.Energy.BaseValue, Owner.Creature, this);
        if (!IsUpgraded || Owner.Creature.HasPower<GalaxyWishRecyclePower>())
            return;

        await PowerCmd.Apply<GalaxyWishRecyclePower>(ctx, Owner.Creature, 1, Owner.Creature, this);
        foreach (var card in KirbyMouth.LivePiles.SelectMany(pile => pile.GetPile(Owner).Cards).OfType<AbilityCard>())
            card.RemoveKeyword(CardKeyword.Exhaust);
    }
}

/// <summary>能力保护罩 (power): gain 4(6) Ability Conservation.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class AbilityShield() : KirbyCard(1, CardType.Power, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<AbilityConservationPower>(4)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<AbilityConservationPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await PowerCmd.Apply<AbilityConservationPower>(ctx, Owner.Creature,
            DynamicVars[nameof(AbilityConservationPower)].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars[nameof(AbilityConservationPower)].UpgradeValueBy(2);
}

/// <summary>混合能力 (skill): gain a random copy ability. Upgrade: cost 0.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class MixAbility() : KirbyCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var kind = Owner.RunState.Rng.CombatCardGeneration.NextItem(CopyAbilities.All);
        await CopyAbilities.Gain(ctx, Owner, kind, upgraded: false, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>帝帝帝吸入 (power): gain Enhanced Inhale. Upgrade: cost 0.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class DededeInhale() : KirbyCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<EnhancedInhalePower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await PowerCmd.Apply<EnhancedInhalePower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>究极之果 (power): the inhaled enemy's first hit deals 0 (still becomes Mouthful). Upgrade: cost 1.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class UltimateFruit() : KirbyCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<UltimateFruitPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await PowerCmd.Apply<UltimateFruitPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

/// <summary>重返梦幻岛: choose 1 of 3 random copy ability cards (upgraded: upgraded ones) and shuffle it into your draw pile. Exhaust.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class ReturnToDreamLand() : KirbyCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const int Choices = 3;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var choice = await ChooseAbilityCard(ctx, Owner, IsUpgraded);
        if (choice != null)
            await CardPileCmd.AddGeneratedCardToCombat(choice, PileType.Draw, Owner, CardPilePosition.Random);
    }

    /// <summary>Choose 1 of 3 random copy ability cards (all different, upgraded if asked). Also the Copy Ability Potion.</summary>
    public static async Task<CardModel?> ChooseAbilityCard(PlayerChoiceContext ctx, Player owner, bool upgraded)
    {
        var rng = owner.RunState.Rng.CombatCardGeneration;
        var pool = CopyAbilities.All.ToList();
        var options = new List<CardModel>();
        while (options.Count < Choices && pool.Count > 0)
        {
            var kind = rng.NextItem(pool);
            pool.Remove(kind);
            var card = CopyAbilities.CreateAbilityCard(owner, kind);
            if (upgraded && card.IsUpgradable)
                CardCmd.Upgrade(card);
            options.Add(card);
        }

        return await CardSelectCmd.FromChooseACardScreen(ctx, options, owner);
    }

    // Upgrade only offers upgraded cards (see OnPlay).
    protected override void OnUpgrade()
    {
    }
}
