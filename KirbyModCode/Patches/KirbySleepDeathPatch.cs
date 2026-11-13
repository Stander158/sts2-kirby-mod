using HarmonyLib;
using KirbyMod.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace KirbyMod.Patches;

/// <summary>
/// Kirby has no death animation of his own (his body is a sprite, not a Spine skeleton): when he dies he falls asleep
/// (KirbySprite.StartSleeping), and wakes up to his normal sprite when revived. Same methods in 0.107 and 0.113.
/// </summary>
[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
public static class KirbySleepDeathPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance) => KirbySprite.StartSleeping(__instance.Entity);
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartReviveAnim))]
public static class KirbySleepRevivePatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance) => KirbySprite.StopSleeping(__instance.Entity);
}
