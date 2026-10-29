using Godot;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace KirbyMod.Mechanics;

/// <summary>Hit effects shared by several attacks, borrowed from base-game cards, plus a full-screen flash.</summary>
internal static class KirbyFx
{
    /// <summary>Defect's Lightning orb evoke.</summary>
    public const string LightningSfx = "event:/sfx/characters/defect/defect_lightning_evoke";

    /// <summary>Burn, Fiend Fire.</summary>
    public const string FireSfx = "event:/sfx/characters/attack_fire";

    /// <summary>A quick blade hit (the Kin's minion).</summary>
    public const string SlashSfx = "event:/sfx/enemy/enemy_attacks/the_kin_minion/the_kin_minion_quick_slash";

    /// <summary>Plasma attacks: a lightning strike with its sound.</summary>
    public static AttackCommand WithLightningHit(this AttackCommand attack) =>
        attack.WithHitFx("vfx/vfx_attack_lightning", LightningSfx);

    /// <summary>Played Burns: the ground fire Burn shows at end of turn, with its sound.</summary>
    public static AttackCommand WithBurnHit(this AttackCommand attack) =>
        attack.WithHitFx(sfx: FireSfx).WithHitVfxNode(target => NGroundFireVfx.Create(target));

    /// <summary>The whole screen flashes <paramref name="color" /> and fades out over <paramref name="seconds" />.</summary>
    public static void ScreenFlash(Color color, float seconds)
    {
        if (NGame.Instance is not { } game)
            return;

        var layer = new CanvasLayer { Layer = 120 };
        var rect = new ColorRect { Color = color, MouseFilter = Control.MouseFilterEnum.Ignore };
        rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        layer.AddChild(rect);
        rect.TreeEntered += () =>
        {
            var tween = rect.CreateTween();
            tween.TweenProperty(rect, "modulate:a", 0f, seconds).SetEase(Tween.EaseType.In);
            tween.TweenCallback(Callable.From(layer.QueueFree));
        };
        game.AddChildSafely(layer);
    }
}
