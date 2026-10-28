using System.Reflection;
using Godot;
using HarmonyLib;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace KirbyMod.Patches;

/// <summary>
/// Draws the Inhaled threshold on an enemy's health bar in yellow, the way the game draws Doom: the left part of the
/// bar up to the threshold, or the whole bar (and yellow HP text) when the enemy is already low enough to die.
/// </summary>
[HarmonyPatch(typeof(NHealthBar))]
public static class InhaledHealthBarPatch
{
    private const string NodeName = "KirbyInhaledForeground";

    private static readonly Color BarColor = new("FFD23F");
    private static readonly Color FontColor = new("FFE96B");
    private static readonly Color FontOutlineColor = new("5C4100");

    private static readonly FieldInfo CreatureField = AccessTools.Field(typeof(NHealthBar), "_creature");
    private static readonly FieldInfo HpForegroundField = AccessTools.Field(typeof(NHealthBar), "_hpForeground");
    private static readonly FieldInfo HpLabelField = AccessTools.Field(typeof(NHealthBar), "_hpLabel");
    private static readonly PropertyInfo MaxFgWidthProperty = AccessTools.Property(typeof(NHealthBar), "MaxFgWidth");
    private static readonly MethodInfo GetFgWidthMethod =
        AccessTools.Method(typeof(NHealthBar), "GetFgWidth", [typeof(int)]);

    [HarmonyPostfix]
    [HarmonyPatch("RefreshForeground")]
    public static void AfterRefreshForeground(NHealthBar __instance)
    {
        if (CreatureField.GetValue(__instance) is not Creature creature
            || HpForegroundField.GetValue(__instance) is not Control hpForeground)
            return;

        var threshold = InhaledPowerBase.ThresholdOn(creature);
        var bar = hpForeground.GetParent()?.GetNodeOrNull<Control>(NodeName);
        if (threshold <= 0 || creature.CurrentHp <= 0 || creature.HpDisplay.IsInfinite())
        {
            if (bar != null)
                bar.Visible = false;
            return;
        }

        bar ??= CreateBar(hpForeground);
        var maxWidth = (float)MaxFgWidthProperty.GetValue(__instance)!;
        var lethal = creature.CurrentHp <= threshold;
        var width = (float)GetFgWidthMethod.Invoke(__instance, [lethal ? creature.CurrentHp : threshold])!;

        bar.Visible = true;
        bar.OffsetLeft = 0f;
        bar.OffsetRight = width - maxWidth;
        if (lethal)
            hpForeground.Visible = false;
    }

    [HarmonyPostfix]
    [HarmonyPatch("RefreshText")]
    public static void AfterRefreshText(NHealthBar __instance)
    {
        if (CreatureField.GetValue(__instance) is not Creature creature || creature.CurrentHp <= 0
            || !creature.HpDisplay.ShowsNumbers() || creature.HpDisplay.IsInfinite()
            || creature.CurrentHp > InhaledPowerBase.ThresholdOn(creature)
            || HpLabelField.GetValue(__instance) is not Control label)
            return;

        label.AddThemeColorOverride("font_color", FontColor);
        label.AddThemeColorOverride("font_outline_color", FontOutlineColor);
    }

    /// <summary>A copy of the red HP foreground, drawn just above it and tinted yellow.</summary>
    private static Control CreateBar(Control hpForeground)
    {
        var bar = (Control)hpForeground.Duplicate();
        bar.Name = NodeName;
        bar.SelfModulate = BarColor;
        foreach (var child in bar.GetChildren())
            child.QueueFree();

        var parent = hpForeground.GetParent();
        parent.AddChild(bar);
        parent.MoveChild(bar, hpForeground.GetIndex() + 1);
        return bar;
    }
}
