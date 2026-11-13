using System.Reflection;
using HarmonyLib;
using KirbyMod.Characters;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace KirbyMod.Patches;

/// <summary>
/// A copy ability's hover tip (on the cards that give it, and on the ability badge) shows the ability's own badge icon,
/// the one drawn above the energy counter, instead of the power's icon (mostly base-game placeholders). HoverTip is a
/// record struct whose Icon has a private setter, so it is set on a boxed copy. Same in 0.107 and 0.113.
/// </summary>
[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.GetDumbHoverTip))]
public static class CopyAbilityTipIconPatch
{
    private static readonly PropertyInfo? IconProperty = AccessTools.Property(typeof(HoverTip), nameof(HoverTip.Icon));

    [HarmonyPostfix]
    public static void Postfix(PowerModel __instance, ref HoverTip __result)
    {
        if (__instance is not CopyAbilityPower ability || IconProperty == null
            || KirbyAbilityHud.BadgeTexture(ability) is not { } icon)
            return;

        object boxed = __result;
        IconProperty.SetValue(boxed, icon);
        __result = (HoverTip)boxed;
    }
}
