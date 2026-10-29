using KirbyMod.Characters;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Cards;

/// <summary>
/// The five cards V-O-I-D puts into your hand: colorless, Ancient frame, 1 cost, with garbled names and only a hint of
/// what they do. Named after Void Termina's moves.
/// </summary>
public abstract class VoidMoveCard(CardType type, TargetType target)
    : ModCardTemplate(1, type, CardRarity.Ancient, target)
{
    public static readonly Type[] All =
        [typeof(VoidShooterCutter), typeof(VoidLoudSong), typeof(VoidDeadlySun), typeof(VoidPleasantDream), typeof(VoidDefeat)];

    public override CardAssetProfile AssetProfile => KirbyArt.ForCard(GetType());

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [TrueEffect(this)];

    /// <summary>What the card really does, in a tip written in Zalgo text (<c>trueTitle</c> / <c>trueDescription</c>).</summary>
    public static IHoverTip TrueEffect(CardModel card) =>
        new HoverTip(new LocString("cards", card.Id.Entry + ".trueTitle"), new LocString("cards", card.Id.Entry + ".trueDescription"));
}

/// <summary>Shooter Cutter: 1 damage to a random enemy 9(11) times.</summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class VoidShooterCutter() : VoidMoveCard(CardType.Attack, TargetType.RandomEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(1, ValueProp.Move), new RepeatVar(9)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(DynamicVars.Repeat.IntValue).FromCard(this)
            .TargetingRandomOpponents(CombatState!).WithHitFx("vfx/vfx_flying_slash").Execute(ctx);
    }

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(2);
}

/// <summary>Loud Song: 2 damage 8(10) times.</summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class VoidLoudSong() : VoidMoveCard(CardType.Attack, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2, ValueProp.Move), new RepeatVar(8)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(DynamicVars.Repeat.IntValue).FromCard(this)
            .Targeting(cardPlay.Target!).WithHitFx("vfx/vfx_hyperbeam_impact").Execute(ctx);
    }

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(2);
}

/// <summary>Deadly Sun: gain 3 energy, then draw until your hand is full.</summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class VoidDeadlySun() : VoidMoveCard(CardType.Skill, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        var room = CardPile.MaxCardsInHand - PileType.Hand.GetPile(Owner).Cards.Count;
        if (room > 0)
            await CardPileCmd.Draw(ctx, room, Owner);
    }
}

/// <summary>Pleasant Dream: stun an enemy, with a loud thud.</summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class VoidPleasantDream() : VoidMoveCard(CardType.Skill, TargetType.AnyEnemy)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/ceremonial_beast/ceremonial_beast_stun");
        await CreatureCmd.Stun(cardPlay.Target!);
    }
}

/// <summary>Void Defeat (power): while Void's 60 hits land next turn, each one also takes 1 HP from every enemy.</summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class VoidDefeat() : VoidMoveCard(CardType.Power, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        if (!Owner.Creature.HasPower<VoidDefeatPower>())
            await PowerCmd.Apply<VoidDefeatPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }
}
