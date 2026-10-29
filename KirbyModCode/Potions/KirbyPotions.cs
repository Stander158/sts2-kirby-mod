using KirbyMod.Cards;
using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Potions;

// Kirby's own potions (his potion pool, so only he finds them). The shop prices them by rarity: Common 50, Rare 100.

/// <summary>星星药水 (Common): gain 20 Mouthful and put a Star Spit into your hand.</summary>
[RegisterPotion(typeof(KirbyPotionPool))]
public sealed class KirbyStarPotion : ModPotionTemplate
{
    public const int Mouthful = 20;

    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    public override PotionAssetProfile AssetProfile => KirbyArt.Potion("star_potion");

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Mouthful", Mouthful)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<MouthfulPower>(), HoverTipFactory.FromCard<SpitStar>()];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var kirby = Owner.Creature;
        await PowerCmd.Apply<MouthfulPower>(choiceContext, kirby, DynamicVars["Mouthful"].BaseValue, kirby, null);
        await KirbyMouth.AddToHandIfMissing<SpitStar>(Owner);
    }
}

/// <summary>复制能力药水 (Common): like Return to Dream Land used to be: choose 1 of 3 random copy ability cards; it goes to your hand and costs 0 this turn.</summary>
[RegisterPotion(typeof(KirbyPotionPool))]
public sealed class CopyAbilityPotion : ModPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    public override PotionAssetProfile AssetProfile => KirbyArt.Potion("copy_ability_potion");

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var choice = await ReturnToDreamLand.ChooseAbilityCard(choiceContext, Owner, upgraded: false);
        if (choice == null)
            return;

        choice.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardToCombat(choice, PileType.Hand, Owner);
    }
}

/// <summary>虚无精华 (Rare): put a V-O-I-D into your hand.</summary>
[RegisterPotion(typeof(KirbyPotionPool))]
public sealed class VoidEssence : ModPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    public override PotionAssetProfile AssetProfile => KirbyArt.Potion("void_essence");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<VoidCard>()];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        await CardPileCmd.AddGeneratedCardToCombat(KirbyMouth.CreateCard<VoidCard>(Owner, false), PileType.Hand, Owner);
    }
}
