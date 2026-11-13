using Godot;
using HarmonyLib;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace KirbyMod.Patches;

/// <summary>
/// Gaining Mouthful shows no big star icon on Kirby: the game's power-applied effect (NPowerAppliedVfx, same in 0.107
/// and 0.113) keeps its floating "满嘴" text, buff sparkle and sound, but its Icon node (and the IconEcho under it) is
/// hidden.
/// </summary>
[HarmonyPatch(typeof(NPowerAppliedVfx), nameof(NPowerAppliedVfx._Ready))]
public static class MouthfulNoFlashPatch
{
    private static readonly AccessTools.FieldRef<NPowerAppliedVfx, PowerModel> Power =
        AccessTools.FieldRefAccess<NPowerAppliedVfx, PowerModel>("_power");

    [HarmonyPostfix]
    public static void Postfix(NPowerAppliedVfx __instance)
    {
        if (Power(__instance) is not MouthfulPower)
            return;
        if (__instance.GetNodeOrNull<CanvasItem>("Icon") is { } icon)
            icon.Visible = false;
    }
}
