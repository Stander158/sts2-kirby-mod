using HarmonyLib;
using KirbyMod.Characters;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;

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

/// <summary>
/// Outside combat the game-over screen makes new player visuals (event, rest site) or reuses the shop ones and plays
/// their Spine "die" animation, which Kirby's sprite does not have: he falls asleep there instead.
/// </summary>
[HarmonyPatch(typeof(NGameOverScreen), "MoveCreaturesToDifferentLayerAndDisableUi")]
public static class KirbyGameOverSleepPatch
{
    [HarmonyPostfix]
    public static void Postfix(Control ____creatureContainer)
    {
        if (____creatureContainer != null)
            KirbySprite.StartSleepingUnder(____creatureContainer);
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartReviveAnim))]
public static class KirbySleepRevivePatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance) => KirbySprite.StopSleeping(__instance.Entity);
}
