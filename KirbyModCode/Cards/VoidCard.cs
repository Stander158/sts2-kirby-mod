using Godot;
using KirbyMod.Characters;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Cards;

/// <summary>
/// V-O-I-D (Ancient frame, from Darv's Dusty Tome): 3(2) cost, Ethereal, Exhaust; its text is garbled on purpose. In
/// order: lose every copy ability (other powers such as Smash Bros. Kirby stay); discard your hand except Drop Ability;
/// every ability's badge appears one after another, for show only; gain the Void ability (<see cref="VoidPower" />,
/// leftmost) with a shake and a flash, and 5 energy; the other badges vanish; add one of each Void card (upgraded with
/// this), Ethereal and Exhaust; and at the start of your next turn Void's 60 hits land (<see cref="VoidDoomPower" />).
/// </summary>
[RegisterCard(typeof(KirbyCardPool))]
[RegisterDustyTomeCard(typeof(Kirby))]
public sealed class VoidCard() : ModCardTemplate(3, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    private const float BadgeDelay = 0.16f;

    public override CardAssetProfile AssetProfile => KirbyArt.ForCard(GetType());

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(5)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<VoidPower>(), HoverTipFactory.FromPower<VoidDoomPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await KirbyMouth.LoseCopyAbility(ctx, Owner);

        var hand = PileType.Hand.GetPile(Owner).Cards.Where(card => card != this && card is not DropAbility).ToList();
        await CardCmd.Discard(ctx, hand);

        foreach (var kind in CopyAbilities.All)
        {
            KirbyAbilityHud.ShowPhantom(Owner, kind);
            await Cmd.Wait(BadgeDelay);
        }

        NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Short);
        KirbyFx.ScreenFlash(Colors.White, 0.7f);
        await CopyAbilities.Gain(ctx, Owner, CopyAbilityKind.Void, upgraded: false, this);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        await Cmd.Wait(0.6f);
        KirbyAbilityHud.ClearPhantoms(Owner);

        foreach (var type in VoidMoveCard.All)
        {
            var card = KirbyMouth.CreateCard(type, Owner, IsUpgraded);
            card.AddKeyword(CardKeyword.Ethereal);
            card.AddKeyword(CardKeyword.Exhaust);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
        }

        if (!Owner.Creature.HasPower<VoidDoomPower>())
            await PowerCmd.Apply<VoidDoomPower>(ctx, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
