using Godot;
using KirbyMod.Characters;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
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
    public const string BoomerangSound = "boomerang.wav";
    public const string LaunchSound = "launch.wav";
    public const string ForceBlastSound = "force_blast.wav";
    public const string BeamSound = "beam.wav";
    public const string TimeChargeBeamSound = "time_charge_beam.wav";

    /// <summary>Played by the character select screen, which only takes an event path (registered in Entry).</summary>
    public const string SelectSfx = "event:/sfx/kirby/select";
    public const string SelectSound = "select.wav";

    public const string SoundFolder = "res://KirbyMod/audio/";
    public const float SoundVolume = 0.6f;

    /// <summary>Plays one of Kirby's own sounds, at the game's SFX volume.</summary>
    /// <param name="whileCombatEnds">Also while the combat is ending (normally silent then).</param>
    public static void PlaySound(string file, bool whileCombatEnds = false)
    {
        if (NonInteractiveMode.IsActive || (CombatManager.Instance.IsEnding && !whileCombatEnds))
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

    /// <summary>Spit Star's bullet: Kirby's energy star.</summary>
    public const string StarBulletTexture = KirbyTheme.CharacterImages + "energy_counter.png";

    /// <summary>
    /// Spit Star: a spinning star (Kirby's energy icon) flies from Kirby's mouth at a constant speed and size. At one
    /// target it stops on the target's centre. Hitting every enemy (<paramref name="passThrough" />), a single star flies
    /// through all of them, aimed at their average centre, and on off the screen. Returns when the star reaches the
    /// target (the farthest one when passing through), so the hit lands with it; the star keeps flying.
    /// </summary>
    public static async Task StarBullet(Creature shooter, IReadOnlyList<Creature> targets, bool passThrough)
    {
        var room = NCombatRoom.Instance;
        var shooterNode = room?.GetCreatureNode(shooter);
        if (room == null || shooterNode == null || NonInteractiveMode.IsActive || !ResourceLoader.Exists(StarBulletTexture))
            return;

        var centres = targets.Select(room.GetCreatureNode).OfType<NCreature>()
            .Select(node => node.Hitbox.GetGlobalRect().GetCenter()).ToList();
        if (centres.Count == 0)
            return;

        const float speed = 2400f;       // px per second
        const float spinPerSecond = 3f;  // turns per second
        const float scale = 1f;  // the energy counter's size (its star is drawn at 128 px)
        var kirby = shooterNode.Hitbox.GetGlobalRect();
        // The mouth is on the side Kirby faces (the game turns him left in the Kaiser Crab fight).
        var facingLeft = KirbySprite.FacesLeft(shooter);
        var mouth = kirby.Position + new Vector2(kirby.Size.X * (facingLeft ? 0.2f : 0.8f), kirby.Size.Y * 0.5f);

        Vector2 end;
        float hitDistance;
        if (passThrough)
        {
            var aim = centres.Aggregate(Vector2.Zero, (sum, c) => sum + c) / centres.Count;
            // Flies the way Kirby faces, out of that side of the screen.
            var side = facingLeft ? -1f : 1f;
            var direction = (aim - mouth).Normalized();
            if (direction.X * side <= 0.1f)
                direction = new Vector2(side, 0f);
            hitDistance = centres.Max(c => (c - mouth).Dot(direction));
            var visible = room.GetViewport().GetVisibleRect();
            var offScreen = facingLeft ? visible.Position.X - 150f : visible.End.X + 150f;
            end = mouth + direction * ((offScreen - mouth.X) / direction.X);
        }
        else
        {
            end = centres[0];
            hitDistance = (end - mouth).Length();
        }

        var duration = (end - mouth).Length() / speed;
        var container = room.CombatVfxContainer;
        var toLocal = container.GetGlobalTransform().AffineInverse();
        var star = new Sprite2D { Texture = ResourceLoader.Load<Texture2D>(StarBulletTexture), Position = toLocal * mouth,
            Scale = Vector2.One * scale };
        star.TreeEntered += () =>
        {
            var tween = star.CreateTween().SetParallel();
            tween.TweenProperty(star, "position", toLocal * end, duration);
            tween.TweenProperty(star, "rotation", Mathf.Tau * spinPerSecond * duration, duration);
            tween.Chain().TweenCallback(Callable.From(star.QueueFree));
        };
        container.AddChildSafely(star);
        await Cmd.Wait(Math.Max(hitDistance, 0f) / speed);
    }

    /// <summary>
    /// Wheel Crush: a tailwind. Kirby dashes towards the way he faces and back, dust puffs at his feet, and white wind
    /// streaks sweep from behind him past <paramref name="targets" /> (all of them when it hits every enemy). Returns
    /// as Kirby reaches the end of his dash, so the hit lands then; the streaks keep flying. Purely visual (its
    /// randomness is not the game's RNG).
    /// </summary>
    public static async Task Tailwind(Creature kirby, IReadOnlyList<Creature> targets)
    {
        var room = NCombatRoom.Instance;
        var node = room?.GetCreatureNode(kirby);
        if (room == null || node == null || NonInteractiveMode.IsActive)
            return;

        const int streaks = 9;
        const float dash = 120f, dashIn = 0.12f, dashBack = 0.25f;
        var side = KirbySprite.FacesLeft(kirby) ? -1f : 1f;
        var self = node.Hitbox.GetGlobalRect();
        var rects = targets.Select(room.GetCreatureNode).OfType<NCreature>().Select(n => n.Hitbox.GetGlobalRect()).ToList();
        var farX = rects.Count == 0 ? self.GetCenter().X + side * 700f
            : side > 0 ? rects.Max(r => r.End.X) : rects.Min(r => r.Position.X);
        var top = rects.Count == 0 ? self.Position.Y : Math.Min(self.Position.Y, rects.Min(r => r.Position.Y));
        var bottom = self.End.Y;
        var startX = side > 0 ? self.Position.X : self.End.X;

        var container = room.CombatVfxContainer;
        var toLocal = container.GetGlobalTransform().AffineInverse();
        var rng = new Random();
        for (var i = 0; i < streaks; i++)
        {
            var length = 140f + rng.NextSingle() * 180f;
            var y = Mathf.Lerp(top + 20f, bottom - 10f, rng.NextSingle());
            var from = new Vector2(startX - side * (60f + rng.NextSingle() * 220f), y);
            var to = new Vector2(farX + side * (150f + rng.NextSingle() * 150f), y);
            var gradient = new Gradient();
            gradient.SetColor(0, new Color(1f, 1f, 1f, 0f));
            gradient.SetColor(1, new Color(1f, 1f, 1f, 0.85f));
            var streak = new Line2D
            {
                Points = [Vector2.Zero, new Vector2(side * length, 0f)], Width = 3f + rng.NextSingle() * 3f,
                Gradient = gradient, Position = toLocal * from, Modulate = new Color(1f, 1f, 1f, 0f),
                BeginCapMode = Line2D.LineCapMode.Round, EndCapMode = Line2D.LineCapMode.Round,
            };
            var delay = i * 0.03f;
            var duration = 0.3f + rng.NextSingle() * 0.12f;
            streak.TreeEntered += () =>
            {
                var tween = streak.CreateTween();
                tween.TweenInterval(delay);
                tween.TweenProperty(streak, "modulate:a", 1f, 0.06f);
                tween.Parallel().TweenProperty(streak, "position", toLocal * to, duration).SetEase(Tween.EaseType.Out);
                tween.Parallel().TweenProperty(streak, "modulate:a", 0f, duration * 0.4f).SetDelay(duration * 0.6f);
                tween.TweenCallback(Callable.From(streak.QueueFree));
            };
            container.AddChildSafely(streak);
        }

        VfxCmd.PlayOnCreature(kirby, "vfx/vfx_smoke_puff");
        if (node.Body is { } body)
        {
            var home = body.Position;
            var tween = body.CreateTween();
            tween.TweenProperty(body, "position", home + new Vector2(side * dash, 0f), dashIn).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(body, "position", home, dashBack).SetEase(Tween.EaseType.InOut);
        }

        await Cmd.Wait(dashIn);
    }
}
