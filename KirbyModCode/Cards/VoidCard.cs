using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Cards;

/// <summary>
/// V-O-I-D (Ancient frame, from Darv's Dusty Tome): 3(2) cost, Ethereal, Exhaust; its text is garbled on purpose. Lose
/// every copy ability and become Void (<see cref="VoidPower" />), gain 5 energy, discard your hand and add the five Void
/// cards (upgraded with this). At the start of your next turn, Void's 60 hits land (<see cref="VoidDoomPower" />).
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
[RegisterDustyTomeCard(typeof(Kirby))]
public sealed class VoidCard() : ModCardTemplate(3, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    public override CardAssetProfile AssetProfile => KirbyArt.ForCard(GetType());

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(5)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [VoidMoveCard.TrueEffect(this), HoverTipFactory.FromPower<VoidPower>(), HoverTipFactory.FromPower<VoidDoomPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        // Every ability goes, Smash Bros. Kirby or not, and Void takes their place.
        await KirbyMouth.LoseCopyAbility(ctx, Owner);
        await CopyAbilities.Gain(ctx, Owner, CopyAbilityKind.Void, upgraded: false, this);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);

        await CardCmd.Discard(ctx, PileType.Hand.GetPile(Owner).Cards.Where(card => card != this).ToList());
        foreach (var type in VoidMoveCard.All)
            await CardPileCmd.AddGeneratedCardToCombat(KirbyMouth.CreateCard(type, Owner, IsUpgraded), PileType.Hand, Owner);

        if (!Owner.Creature.HasPower<VoidDoomPower>())
            await PowerCmd.Apply<VoidDoomPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
