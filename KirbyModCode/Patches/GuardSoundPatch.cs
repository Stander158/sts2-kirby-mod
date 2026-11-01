using HarmonyLib;
using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.ValueProps;

namespace KirbyMod.Patches;

/// <summary>
/// Kirby gains block with his own guard sound. GainBlock is async, so the flag is an AsyncLocal: set before the call, it
/// flows through every await inside it, while the postfix clears it for the caller only.
/// </summary>
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.GainBlock),
    [typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardPlay), typeof(bool)])]
public static class GuardSoundPatch
{
    internal static readonly AsyncLocal<bool> KirbyGainingBlock = new();

    [HarmonyPrefix]
    public static void Prefix(Creature creature) => KirbyGainingBlock.Value = creature.Player?.Character is Kirby;

    [HarmonyPostfix]
    public static void Postfix() => KirbyGainingBlock.Value = false;
}

[HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.PlayOneShot), [typeof(string), typeof(float)])]
public static class GuardSoundReplacePatch
{
    private const string BlockGainSfx = "event:/sfx/block_gain";

    [HarmonyPrefix]
    public static bool Prefix(string path)
    {
        if (path != BlockGainSfx || !GuardSoundPatch.KirbyGainingBlock.Value)
            return true;

        KirbyFx.PlaySound(KirbyFx.GuardSound);
        return false;
    }
}
