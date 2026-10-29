using Godot;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace KirbyMod.Characters;

/// <summary>
/// Badges for Kirby's current copy abilities, to the right of the energy counter (several with Smash Bros. Kirby or
/// Void). A badge pops in when its ability is gained; hovering one shows the ability's tip. It is a child of the energy
/// counter, so it slides in and out with it.
/// </summary>
internal static class KirbyAbilityHud
{
    private const string Badges = "res://KirbyMod/images/abilities/";
    private const float BadgeSize = 64f;
    private static readonly Vector2 Offset = new(122f, 30f);

    public static void Attach(Control energyCounter, Player player)
    {
        var row = new HBoxContainer { Name = "KirbyAbilityHud", Position = Offset, MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 4);

        List<CopyAbilityPower> shown = [];
        void Update()
        {
            var held = player.Creature.CombatState == null ? [] : CopyAbilities.Held(player);
            if (held.SequenceEqual(shown))
                return;

            var gained = held.Except(shown).ToHashSet();
            shown = held.ToList();
            foreach (var child in row.GetChildren())
                child.QueueFree();
            foreach (var power in shown)
                row.AddChild(Badge(power, gained.Contains(power)));
        }

        row.TreeEntered += () => row.GetTree().ProcessFrame += Update;
        row.TreeExiting += () => row.GetTree().ProcessFrame -= Update;
        energyCounter.AddChild(row);
    }

    private static TextureRect Badge(CopyAbilityPower power, bool pop)
    {
        var badge = new TextureRect
        {
            Texture = BadgeTexture(power),
            CustomMinimumSize = new Vector2(BadgeSize, BadgeSize),
            PivotOffset = new Vector2(BadgeSize, BadgeSize) / 2,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        badge.MouseEntered += () => NHoverTipSet.CreateAndShow(badge, HoverTipFactory.FromPower(power));
        badge.MouseExited += () => NHoverTipSet.Remove(badge);

        if (pop)
        {
            badge.Scale = Vector2.Zero;
            badge.TreeEntered += () =>
            {
                var tween = badge.CreateTween();
                tween.TweenProperty(badge, "scale", Vector2.One * 1.25f, 0.18).SetEase(Tween.EaseType.Out);
                tween.TweenProperty(badge, "scale", Vector2.One, 0.12).SetEase(Tween.EaseType.In);
            };
        }

        return badge;
    }

    /// <summary>The ability's badge, or its power icon when the badges are not there (public build).</summary>
    private static Texture2D? BadgeTexture(CopyAbilityPower power)
    {
        var badge = Badges + SnakeCase(power.Kind.ToString()) + ".png";
        var path = ResourceLoader.Exists(badge) ? badge : power.AssetProfile.BigIconPath ?? power.AssetProfile.IconPath;
        return path != null && ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
    }

    private static string SnakeCase(string name) =>
        string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
}
