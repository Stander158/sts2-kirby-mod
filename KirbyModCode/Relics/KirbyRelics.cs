using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Relics;

/// <summary>能力保护器 (Common): at the start of combat, gain 3 Ability Conservation.</summary>
[RegisterRelic(typeof(KirbyRelicPool))]
public sealed class AbilityProtector : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => KirbyArt.VanillaRelic("anchor");

    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<AbilityConservationPower>(3)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<AbilityConservationPower>()];

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<AbilityConservationPower>(new BlockingPlayerChoiceContext(), Owner.Creature,
            DynamicVars[nameof(AbilityConservationPower)].BaseValue, Owner.Creature, null);
    }
}

/// <summary>友谊之星 (Uncommon): at the end of your turn, if you dealt no damage this turn, apply 1 Weak to ALL enemies.</summary>
[RegisterRelic(typeof(KirbyRelicPool))]
public sealed class FriendshipStar : ModRelicTemplate
{
    private bool _dealtDamage;

    public override RelicAssetProfile AssetProfile => KirbyArt.VanillaRelic("red_mask");

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<WeakPower>(1)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<WeakPower>()];

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner.Creature))
            _dealtDamage = false;
        return Task.CompletedTask;
    }

    public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        if ((dealer == Owner.Creature || dealer?.PetOwner == Owner) && !target.IsPlayer && result.TotalDamage > 0)
            _dealtDamage = true;
        return Task.CompletedTask;
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner.Creature) || _dealtDamage)
            return;

        var enemies = Owner.Creature.CombatState?.HittableEnemies.ToList();
        if (enemies == null || enemies.Count == 0)
            return;

        Flash();
        await PowerCmd.Apply<WeakPower>(choiceContext, enemies, DynamicVars["WeakPower"].BaseValue, Owner.Creature, null);
    }
}

/// <summary>Boss准备 (Rare): at the start of combat, choose 1 of 3 copy ability cards to add to hand. It costs 0 this turn.</summary>
[RegisterRelic(typeof(KirbyRelicPool))]
public sealed class BossPrep : ModRelicTemplate
{
    private const int Choices = 3;

    public override RelicAssetProfile AssetProfile => KirbyArt.VanillaRelic("toolbox");

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(Choices)];

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != Owner || Owner.PlayerCombatState?.TurnNumber != 1)
            return;

        Flash();
        var rng = Owner.RunState.Rng.CombatCardGeneration;
        var pool = CopyAbilities.All.ToList();
        var options = new List<CardModel>();
        while (options.Count < Choices && pool.Count > 0)
        {
            var kind = rng.NextItem(pool);
            pool.Remove(kind);
            options.Add(CopyAbilities.CreateAbilityCard(Owner, kind));
        }

        var choice = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, Owner);
        if (choice == null)
            return;

        await CardPileCmd.AddGeneratedCardToCombat(choice, PileType.Hand, Owner);
        choice.SetToFreeThisTurn();
    }
}
