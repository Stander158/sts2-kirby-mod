using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Cards;

/// <summary>
/// 冰块 (made by the Ice ability): pick an enemy, then choose to hit it for 10(14) or gain 8(11) Block. The game has
/// no "self or enemy" target type, hence the follow-up choice. Ice Blocks made each turn stay when Ice is lost; only
/// the one handed out on gaining Ice is tagged and exhausted.
/// </summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class IceBlock() : ModCardTemplate(1, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
{
    public override CardAssetProfile AssetProfile => KirbyArt.ForCard(GetType());

    public const int BaseBlock = 8;
    public const int BaseDamage = 10;
    public const int BlockUpgrade = 3;
    public const int DamageUpgrade = 4;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(BaseBlock, ValueProp.Move), new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        // 究极冰球: do both, no choice.
        if (Owner.Creature.HasPower<Powers.SnowBowlPower>())
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            await Hit(ctx, cardPlay);
            return;
        }

        var options = new CardModel[]
        {
            KirbyMouth.CreateCard<IceBlockStrike>(Owner, IsUpgraded),
            KirbyMouth.CreateCard<IceBlockGuard>(Owner, IsUpgraded),
        };
        var choice = await CardSelectCmd.FromChooseACardScreen(ctx, options, Owner);

        if (choice is IceBlockGuard)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            return;
        }

        await Hit(ctx, cardPlay);
    }

    private Task Hit(PlayerChoiceContext ctx, CardPlay cardPlay) =>
        DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(ctx);

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(BlockUpgrade);
        DynamicVars.Damage.UpgradeValueBy(DamageUpgrade);
    }
}

/// <summary>Display-only option for <see cref="IceBlock" />: hit the chosen enemy.</summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class IceBlockStrike() : ModCardTemplate(1, CardType.Attack, CardRarity.Token, TargetType.None, false)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(IceBlock.BaseDamage, ValueProp.Move)];

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(IceBlock.DamageUpgrade);
}

/// <summary>Display-only option for <see cref="IceBlock" />: gain Block.</summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class IceBlockGuard() : ModCardTemplate(1, CardType.Skill, CardRarity.Token, TargetType.None, false)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(IceBlock.BaseBlock, ValueProp.Move)];

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(IceBlock.BlockUpgrade);
}
