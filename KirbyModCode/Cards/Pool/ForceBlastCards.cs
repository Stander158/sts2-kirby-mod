using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace KirbyMod.Cards;

/// <summary>
/// 波动拳: like the Regent's Forge, if there is no Launch in your hand, add one to it; every Launch in your hand gets
/// +2(3) hits (and +1 damage while you have Fighter). Then it returns to your hand, like Particle Wall.
/// The Launch it makes belongs to Fighter: losing Fighter exhausts it.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class ForceBlast() : KirbyCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    private const int FighterDamageBonus = 1;

    // Fighter boosts Launch, but this card itself is not retained (no SynergyAbilities).
    private static readonly CopyAbilityKind[] Fighter = [CopyAbilityKind.Fighter];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Hits", 2), new DynamicVar("FighterBonus", FighterDamageBonus)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<Launch>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        if (!Launches().Any())
        {
            var launch = KirbyMouth.CreateCard<Launch>(Owner, false);
            CopyAbilities.MarkGranted(launch, CopyAbilityKind.Fighter);
            await CardPileCmd.AddGeneratedCardToCombat(launch, PileType.Hand, Owner);
        }

        var fighter = CopyAbilities.Has(Owner, Fighter);
        foreach (var launch in Launches().ToList())
        {
            launch.DynamicVars.Repeat.UpgradeValueBy(DynamicVars["Hits"].BaseValue);
            if (fighter)
                launch.DynamicVars.Damage.UpgradeValueBy(FighterDamageBonus);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Hits"].UpgradeValueBy(1);

    // Back to hand instead of the discard pile (an Exhaust from elsewhere still wins), like Particle Wall.
    protected override PileType GetResultPileTypeForCardPlay()
    {
        var pile = base.GetResultPileTypeForCardPlay();
        return pile == PileType.Discard ? PileType.Hand : pile;
    }

    // Only the hand counts: a Launch elsewhere is neither boosted nor stops a new one being made.
    private IEnumerable<Launch> Launches() => PileType.Hand.GetPile(Owner).Cards.OfType<Launch>();
}

/// <summary>发射 (made by Force Blast): 0 cost, Ethereal, deal 4(6) damage 0 times. Exhaust. Force Blast adds hits.</summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class Launch() : KirbyCard(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4, ValueProp.Move), new RepeatVar(0)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(DynamicVars.Repeat.IntValue).FromCard(this)
            .Targeting(cardPlay.Target!).WithHitFx("vfx/vfx_attack_blunt").Execute(ctx);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}
