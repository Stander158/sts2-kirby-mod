using HarmonyLib;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace KirbyMod.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.TargetType), MethodType.Getter)]
public static class FireBurnTargetPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref TargetType __result)
    {
        // Canonical cards (compendium, hover previews) throw on Owner access.
        if (__instance is Burn && __instance.IsMutable
            && __instance.Owner?.Creature?.HasPower<FirePower>() == true)
            __result = TargetType.AnyEnemy;
    }
}
