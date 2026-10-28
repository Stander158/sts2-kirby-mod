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
/// 虚无 (Void, Ancient frame, from Darv's Dusty Tome): 3(2) cost, Ethereal, Exhaust. Exhaust your hand, gain every copy
/// ability at once (none replaces another, Sleep does not end the turn), gain 2 energy, and die at the start of your
/// next turn.
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
[RegisterDustyTomeCard(typeof(Kirby))]
public sealed class VoidCard() : ModCardTemplate(3, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    public override CardAssetProfile AssetProfile => KirbyArt.ForCard(GetType());

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<VoidDoomPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        foreach (var card in PileType.Hand.GetPile(Owner).Cards.Where(card => card != this).ToList())
            await CardCmd.Exhaust(ctx, card);

        foreach (var kind in CopyAbilities.All)
            await CopyAbilities.Gain(ctx, Owner, kind, upgraded: false, this, keepOthers: true, allowEndTurn: false);

        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        await PowerCmd.Apply<VoidDoomPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
