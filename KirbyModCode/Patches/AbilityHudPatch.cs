using HarmonyLib;
using KirbyMod.Characters;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace KirbyMod.Patches;

/// <summary>Adds Kirby's copy ability badges next to the energy counter when a combat's UI comes up.</summary>
[HarmonyPatch(typeof(NCombatUi), nameof(NCombatUi.Activate))]
public static class AbilityHudPatch
{
    private static readonly AccessTools.FieldRef<NCombatUi, NEnergyCounter> EnergyCounter =
        AccessTools.FieldRefAccess<NCombatUi, NEnergyCounter>("_energyCounter");

    [HarmonyPostfix]
    public static void Postfix(NCombatUi __instance, CombatState state)
    {
        var me = LocalContext.GetMe(state);
        if (me?.Character is Kirby)
            KirbyAbilityHud.Attach(EnergyCounter(__instance), me);
    }
}
