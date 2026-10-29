using System.Runtime.CompilerServices;
using Godot;
using KirbyMod.Mechanics;
using KirbyMod.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace KirbyMod.Characters;

/// <summary>
/// Kirby's current copy ability as a big badge with its name, at the lower right of the energy counter (one badge per
/// ability with Smash Bros. Kirby). Kirby's head shows while he has none; other characters (given an ability by
/// Ability Offering) only show badges while they hold one. New badges flip in, and hovering a real one shows the
/// ability's tip above it to the left, growing upward so a long tip stays on screen. Badges can also be shown for abilities that are not held (<see cref="ShowPhantom" />,
/// V-O-I-D's display). It is a child of the energy counter, so it slides in and out with it. The abilities themselves
/// are hidden from the status bar (<see cref="CopyAbilityPower" />).
/// </summary>
internal static class KirbyAbilityHud
{
    private const string Badges = "res://KirbyMod/images/abilities/";
    private const string KirbyHead = "res://KirbyMod/images/character/icon.png";
    private const float BadgeSize = 96f;
    private const float BadgeGap = 4f;
    private static readonly Vector2 Origin = new(75f, 75f);
    private const float TipLeft = -70f;
    private const float TipGap = 12f;
    private const float ScreenMargin = 10f;

    /// <summary>Badges shown after the held abilities without the abilities being held.</summary>
    private static readonly ConditionalWeakTable<Player, List<CopyAbilityKind>> Phantoms = new();

    public static void ShowPhantom(Player player, CopyAbilityKind kind) => Phantoms.GetOrCreateValue(player).Add(kind);

    public static void ClearPhantoms(Player player) => Phantoms.GetOrCreateValue(player).Clear();

    private sealed record Entry(CopyAbilityKind Kind, CopyAbilityPower? Held);

    public static void Attach(Control energyCounter, Player player)
    {
        var root = new Control { Name = "KirbyAbilityHud", MouseFilter = Control.MouseFilterEnum.Ignore };
        var isKirby = player.Character is Kirby;

        List<Entry>? shown = null;
        void Update()
        {
            List<Entry> entries = player.Creature.CombatState == null
                ? []
                :
                [
                    .. CopyAbilities.Held(player).Select(power => new Entry(power.Kind, power)),
                    .. Phantoms.GetOrCreateValue(player).Select(kind => new Entry(kind, null)),
                ];
            if (shown != null && entries.SequenceEqual(shown))
                return;

            var before = shown;
            shown = entries;
            foreach (var child in root.GetChildren())
                child.QueueFree();

            if (entries.Count == 0)
            {
                if (isKirby)
                    root.AddChild(Badge(Load(KirbyHead), null, null, 0, before != null));
                return;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var model = entry.Held ?? (CopyAbilityPower)CopyAbilities.CanonicalPower(entry.Kind);
                var isNew = before != null && !before.Contains(entry);
                root.AddChild(Badge(BadgeTexture(model), ShortName(model), entry.Held, i, isNew));
            }
        }

        root.TreeEntered += () => root.GetTree().ProcessFrame += Update;
        root.TreeExiting += () => root.GetTree().ProcessFrame -= Update;
        energyCounter.AddChild(root);
    }

    private static Control Badge(Texture2D? texture, string? name, CopyAbilityPower? power, int index, bool animate)
    {
        var panel = new Control
        {
            Size = new Vector2(BadgeSize, BadgeSize),
            Position = Origin + new Vector2(index * (BadgeSize + BadgeGap), 0f),
            PivotOffset = new Vector2(BadgeSize, BadgeSize) / 2,
            MouseFilter = power == null ? Control.MouseFilterEnum.Ignore : Control.MouseFilterEnum.Stop,
        };

        var icon = new TextureRect
        {
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        icon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        panel.AddChild(icon);

        if (name != null)
        {
            var label = new Label
            {
                Text = name,
                Position = new Vector2(0f, 16f),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                LabelSettings = new LabelSettings
                {
                    FontSize = 28,
                    FontColor = new Color("f6f6f6"),
                    OutlineSize = 12,
                    OutlineColor = new Color("eb7295"),
                },
            };
            label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            panel.AddChild(label);
        }

        if (power != null)
        {
            panel.MouseEntered += () =>
            {
                if (NHoverTipSet.CreateAndShow(panel, HoverTipFactory.FromPower(power)) is { } tips)
                    PlaceAbove(tips, panel);
            };
            panel.MouseExited += () => NHoverTipSet.Remove(panel);
        }

        if (animate)
        {
            panel.Scale = Vector2.Zero;
            panel.TreeEntered += () =>
            {
                var tween = panel.CreateTween();
                tween.TweenProperty(panel, "scale", Vector2.One * 1.2f, 0.25).SetEase(Tween.EaseType.InOut);
                tween.TweenProperty(panel, "scale", Vector2.One, 0.06).SetEase(Tween.EaseType.In);
            };
        }

        return panel;
    }

    /// <summary>
    /// Puts the tip's bottom just above the badge, so it grows upward however long it is (but not past the top of the
    /// screen). The tip's text box is only filled in and laid out after it appears, so it is placed again every frame
    /// until it closes.
    /// </summary>
    private static void PlaceAbove(Control tips, Control panel)
    {
        var tree = panel.GetTree();
        void Place()
        {
            if (!GodotObject.IsInstanceValid(tips) || !GodotObject.IsInstanceValid(panel))
            {
                tree.ProcessFrame -= Place;
                return;
            }
            if (!tips.IsInsideTree())
                return;

            var height = tips.GetNodeOrNull<Control>("textHoverTipContainer")?.Size.Y ?? 0f;
            var y = Math.Max(ScreenMargin, panel.GlobalPosition.Y - height - TipGap);
            tips.GlobalPosition = new Vector2(panel.GlobalPosition.X + TipLeft, y);
        }

        Place();
        tree.ProcessFrame += Place;
    }

    /// <summary>"复制能力：剑" / "Copy Ability: Sword" -> "剑" / "Sword".</summary>
    private static string ShortName(CopyAbilityPower power)
    {
        var title = power.Title.GetFormattedText();
        var cut = Math.Max(title.LastIndexOf('：'), title.LastIndexOf(':'));
        return cut >= 0 ? title[(cut + 1)..].Trim() : title;
    }

    /// <summary>The ability's badge, or its power icon when the badges are not there (public build).</summary>
    private static Texture2D? BadgeTexture(CopyAbilityPower power)
    {
        var badge = Badges + SnakeCase(power.Kind.ToString()) + ".png";
        return Load(ResourceLoader.Exists(badge) ? badge : power.AssetProfile.BigIconPath ?? power.AssetProfile.IconPath);
    }

    private static Texture2D? Load(string? path) =>
        path != null && ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;

    private static string SnakeCase(string name) =>
        string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
}
