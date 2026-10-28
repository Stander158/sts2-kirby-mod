using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace KirbyMod.Cards;

/// <summary>吐息 (0 cost): exhaust a card in hand, then deal 9(13). Ice/Fire: Retain.</summary>
[RegisterCard(typeof(KirbyCardPool))]
public sealed class IceBreath() : KirbyCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IReadOnlyList<CopyAbilityKind> SynergyAbilities => [CopyAbilityKind.Ice, CopyAbilityKind.Fire];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        if (PileType.Hand.GetPile(Owner).Cards.Count > 0)
        {
            var chosen = await CardSelectCmd.FromHand(ctx, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1), null,
                this);
            foreach (var card in chosen.ToList())
                await CardCmd.Exhaust(ctx, card);
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_gaseous_impact").Execute(ctx);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4);
}
