using KirbyMod.Characters;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Cards;

/// <summary>
/// The five cards V-O-I-D puts into your hand (made Ethereal and Exhaust there): colorless, Ancient frame, 1 cost, with
/// names of block characters and only a hint of what they do. Named after Void Termina's moves.
/// </summary>
public abstract class VoidMoveCard(CardType type, TargetType target)
    : ModCardTemplate(1, type, CardRarity.Ancient, target)
{
    public static readonly Type[] All =
        [typeof(VoidShooterCutter), typeof(VoidLoudSong), typeof(VoidDeadlySun), typeof(VoidPleasantDream), typeof(VoidDefeat)];

    public override CardAssetProfile AssetProfile => KirbyArt.ForCard(GetType());
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

/// <summary>Deadly Sun: gain 3(4) energy, then draw until your hand is full.</summary>
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

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1);
}

/// <summary>Pleasant Dream: stun an enemy (upgraded: every enemy), with a loud thud and a shake.</summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class VoidPleasantDream() : VoidMoveCard(CardType.Skill, TargetType.AnyEnemy)
{
    public override TargetType TargetType => IsUpgraded ? TargetType.AllEnemies : TargetType.AnyEnemy;

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/ceremonial_beast/ceremonial_beast_stun");
        NGame.Instance?.ScreenShake(IsUpgraded ? ShakeStrength.Strong : ShakeStrength.Medium, ShakeDuration.Short);

        var targets = IsUpgraded ? CombatState!.HittableEnemies.ToList() : [cardPlay.Target!];
        foreach (var target in targets)
            await CreatureCmd.Stun(target);
    }
}

/// <summary>Void Defeat (power): while Void's 60 hits land next turn, each one also takes 2 HP from every enemy.</summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class VoidDefeat() : VoidMoveCard(CardType.Power, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        if (!Owner.Creature.HasPower<VoidDefeatPower>())
            await PowerCmd.Apply<VoidDefeatPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }
}
