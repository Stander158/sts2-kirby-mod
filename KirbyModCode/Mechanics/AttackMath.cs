using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace KirbyMod.Mechanics;

/// <summary>
/// Helpers for effects that cap an enemy's hit to 0 and then need what the hit would have dealt
/// (Inhale's Mouthful, Mirror Labyrinth's block).
/// </summary>
public static class AttackMath
{
    private static readonly AccessTools.FieldRef<AttackCommand, decimal> DamagePerHit =
        AccessTools.FieldRefAccess<AttackCommand, decimal>("_damagePerHit");

    private static readonly AccessTools.FieldRef<AttackCommand, CalculatedDamageVar?> CalculatedDamage =
        AccessTools.FieldRefAccess<AttackCommand, CalculatedDamageVar?>("_calculatedDamageVar");

    /// <summary>Per-hit damage of the attack before any hooks (strength, vulnerable, ...).</summary>
    public static decimal BaseDamage(AttackCommand command, Creature? target) =>
        CalculatedDamage(command)?.Calculate(target) ?? DamagePerHit(command);

    /// <summary>The hit's damage after the usual additive and multiplicative hooks, ignoring every cap.</summary>
    public static int UncappedDamage(Creature target, Creature dealer, decimal baseDamage, ValueProp props,
        CardModel? cardSource)
    {
        var damage = Hook.ModifyDamage(IRunState.GetFrom([target, dealer]), target.CombatState, target, dealer,
            baseDamage, props, cardSource, ModifyDamageHookType.Additive | ModifyDamageHookType.Multiplicative,
            CardPreviewMode.None, out _);
        return Math.Max(0, (int)damage);
    }
}
