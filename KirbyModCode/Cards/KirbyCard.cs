using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Cards;

/// <summary>
/// Base for Kirby cards. Cards listing <see cref="SynergyAbilities" /> are retained at end of turn while Kirby holds
/// one of those abilities (<see cref="Powers.CopyAbilityPower" />), and may get a bonus via <see cref="SynergyDamage" />.
/// </summary>
public abstract class KirbyCard(int cost, CardType type, CardRarity rarity, TargetType target, bool showInCardLibrary = true)
    : ModCardTemplate(cost, type, rarity, target, showInCardLibrary), IAbilitySynergyCard
{
    public override CardAssetProfile AssetProfile => KirbyArt.ForCard(GetType());

    public virtual IReadOnlyList<CopyAbilityKind> SynergyAbilities => [];

    protected bool SynergyActive => CopyAbilities.Has(Owner, SynergyAbilities);

    /// <summary>
    /// <c>{CalculatedDamage}</c> = <paramref name="baseDamage" />, plus <paramref name="bonus" /> while a synergy
    /// ability is held. Upgrade with <c>CalculationBase</c> / <c>ExtraDamage</c>.
    /// </summary>
    protected static IEnumerable<DynamicVar> SynergyDamage(decimal baseDamage, decimal bonus) =>
    [
        new CalculationBaseVar(baseDamage),
        new ExtraDamageVar(bonus),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(SynergyMultiplier),
    ];

    private static decimal SynergyMultiplier(CardModel card, Creature? _) =>
        card is KirbyCard { SynergyActive: true } ? 1m : 0m;
}
