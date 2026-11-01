using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Audio;

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

    // Kirby's own sounds (KirbyMod/audio, assembled from the private art folder like the card art).
    public const string InhaleSound = "inhale.wav";
    public const string ScanSound = "scan.wav";
    public const string SpitStarSound = "spit_star.wav";
    public const string MouthfulSound = "mouthful.wav";
    public const string GuardSound = "guard.wav";
    public const string SleepSound = "sleep.wav";
    public const string AbilityGainSound = "ability_gain.wav";
    public const string AbilityDropSound = "ability_drop.wav";
    public const string DynabladeSound = "dynablade.wav";

    /// <summary>Played by the character select screen, which only takes an event path (registered in Entry).</summary>
    public const string SelectSfx = "event:/sfx/kirby/select";
    public const string SelectSound = "select.wav";

    public const string SoundFolder = "res://KirbyMod/audio/";
    public const float SoundVolume = 0.6f;

    /// <summary>Plays one of Kirby's own sounds, at the game's SFX volume.</summary>
    public static void PlaySound(string file)
    {
        if (NonInteractiveMode.IsActive || CombatManager.Instance.IsEnding)
            return;

        var sfxVolume = SaveManager.Instance.SettingsSave.VolumeSfx;
        FmodStudioStreamingFiles.TryPlayResourceSound(SoundFolder + file, SoundVolume * sfxVolume * sfxVolume);
    }

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
