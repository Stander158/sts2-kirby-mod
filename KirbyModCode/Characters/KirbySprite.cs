using Godot;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace KirbyMod.Characters;

/// <summary>
/// Kirby's combat sprite, in order of priority: spitting (a moment of open mouth), the latest copy ability he gained and still holds (Void huge), inhaling (not in the
/// Robobot Armor), Mouthful (twice as big), the Robobot Armor, else the normal one. A picture that is not there (public
/// build) is skipped. Ability pictures
/// fill the same canvas whatever else is in them (a long sword, a wheel...), so each has its own scale that brings
/// Kirby's body to the size of the normal sprite (Carby is simply big). The sprite is anchored at Kirby's feet, so bigger
/// ones grow upward. Each state is remembered on the sprite node.
/// When Kirby dies he falls asleep instead (Smash Ultimate's sleep, frames in character/kirby_sleep/): the intro once,
/// then the breathing loop (forward and back), over every other state until he is revived.
/// </summary>
internal static class KirbySprite
{
    private const string Normal = KirbyTheme.CharacterImages + "kirby_combat.png";
    private const string FullMouth = KirbyTheme.CharacterImages + "kirby_mouthful.png";
    private const string Robobot = KirbyTheme.CharacterImages + "kirby_robobot.png";
    private const string Inhaling = KirbyTheme.CharacterImages + "kirby_inhale.png";
    private const string Void = KirbyTheme.CharacterImages + "kirby_void.png";
    private const string AbilityPrefix = KirbyTheme.CharacterImages + "kirby_ability_";
    private const float FullMouthScale = 2f;
    private const float RobobotScale = 2f;
    private const float InhalingScale = 1.1f;
    private const float VoidScale = 2.6f;
    private const string BaseScaleMeta = "kirby_base_scale";
    private const string MouthfulMeta = "kirby_mouthful";
    private const string RobobotMeta = "kirby_robobot";
    private const string InhalingMeta = "kirby_inhaling";
    private const string SpittingMeta = "kirby_spitting";
    private const string SleepingMeta = "kirby_sleeping";
    private const string SleepFolder = KirbyTheme.CharacterImages + "kirby_sleep/";
    private const float SleepScale = 1.7f;
    private const double SleepFps = 30;
    private static readonly Dictionary<ulong, Tween> SleepTweens = new();
    private const string AbilityMeta = "kirby_ability";
    private const string AbilityScaleMeta = "kirby_ability_scale";

    private static readonly Dictionary<CopyAbilityKind, float> AbilityScales = new()
    {
        [CopyAbilityKind.Sword] = 2f,
        [CopyAbilityKind.Boomerang] = 1.25f,
        [CopyAbilityKind.Stone] = 1.9f,
        [CopyAbilityKind.Fighter] = 1.5f,
        [CopyAbilityKind.Ice] = 1.45f,
        [CopyAbilityKind.Fire] = 2f,
        [CopyAbilityKind.Sleep] = 1.2f,
        [CopyAbilityKind.Plasma] = 1.9f,
        [CopyAbilityKind.Car] = 2.2f,
        [CopyAbilityKind.StarRod] = 2.6f,
        [CopyAbilityKind.Galaxia] = 2f,
        [CopyAbilityKind.Doctor] = 1.5f,
        [CopyAbilityKind.Wheel] = 1.2f,
        [CopyAbilityKind.Beam] = 1.7f,
        [CopyAbilityKind.Void] = VoidScale,
    };

    public static void SetMouthful(Creature creature, bool full) => Set(creature, MouthfulMeta, full);

    public static void SetRobobot(Creature creature, bool on) => Set(creature, RobobotMeta, on);

    public static void SetInhaling(Creature creature, bool on) => Set(creature, InhalingMeta, on);

    /// <summary>
    /// Spit Star: the open-mouth (inhale) sprite for <paramref name="seconds" />, over everything else. A newer spit
    /// restarts the time; only the last one switches it back.
    /// </summary>
    public static void ShowSpitting(Creature creature, double seconds)
    {
        if (creature.Player?.Character is not Kirby || creature.GetCreatureNode()?.Body is not Sprite2D sprite
            || !sprite.IsInsideTree())
            return;

        var id = sprite.GetMeta(SpittingMeta, 0).AsInt32() + 1;
        Set(creature, SpittingMeta, id);
        sprite.GetTree().CreateTimer(seconds).Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(sprite) && sprite.GetMeta(SpittingMeta, 0).AsInt32() == id)
                Set(creature, SpittingMeta, -id);
        };
    }

    /// <summary><paramref name="kind" />: the ability that now decides the sprite, or null for none.</summary>
    public static void SetAbility(Creature creature, CopyAbilityKind? kind)
    {
        if (creature.GetCreatureNode()?.Body is Sprite2D sprite)
            sprite.SetMeta(AbilityScaleMeta, kind == null ? 1f : AbilityScales.GetValueOrDefault(kind.Value, 1f));
        Set(creature, AbilityMeta, kind == null ? "" : AbilityPath(kind.Value));
    }

    /// <summary>Kirby died: the sleep animation (intro once, then the breathing loop) replaces his sprite.</summary>
    public static void StartSleeping(Creature creature)
    {
        if (creature.Player?.Character is not Kirby || creature.GetCreatureNode()?.Body is not Sprite2D sprite
            || !sprite.IsInsideTree())
            return;

        var intro = LoadFrames("intro");
        var loop = LoadFrames("loop");
        if (loop.Count == 0)
            return;
        // Breathing plays forward then backward (ping-pong), each end frame shown once per turn.
        if (loop.Count > 2)
            loop.AddRange(Enumerable.Reverse(loop.GetRange(1, loop.Count - 2)).ToList());

        KillSleepTween(sprite);
        if (!sprite.HasMeta(BaseScaleMeta))
            sprite.SetMeta(BaseScaleMeta, sprite.Scale);
        sprite.SetMeta(SleepingMeta, true);
        sprite.Scale = sprite.GetMeta(BaseScaleMeta).AsVector2() * SleepScale;

        var tween = sprite.CreateTween();
        if (intro.Count > 0)
            tween.TweenMethod(Callable.From<int>(i => sprite.Texture = intro[Math.Min(i, intro.Count - 1)]), 0,
                intro.Count, intro.Count / SleepFps);
        tween.TweenCallback(Callable.From(() =>
        {
            var breathing = sprite.CreateTween().SetLoops();
            breathing.TweenMethod(Callable.From<int>(i => sprite.Texture = loop[Math.Min(i, loop.Count - 1)]), 0,
                loop.Count, loop.Count / SleepFps);
            SleepTweens[sprite.GetInstanceId()] = breathing;
        }));
        SleepTweens[sprite.GetInstanceId()] = tween;
    }

    /// <summary>Kirby was revived: back to the sprite of his current state.</summary>
    public static void StopSleeping(Creature creature)
    {
        if (creature.Player?.Character is not Kirby || creature.GetCreatureNode()?.Body is not Sprite2D sprite
            || !sprite.GetMeta(SleepingMeta, false).AsBool())
            return;

        KillSleepTween(sprite);
        Set(creature, SleepingMeta, false);
    }

    private static void KillSleepTween(Sprite2D sprite)
    {
        if (SleepTweens.Remove(sprite.GetInstanceId(), out var tween) && GodotObject.IsInstanceValid(tween))
            tween.Kill();
    }

    private static List<Texture2D> LoadFrames(string prefix)
    {
        var frames = new List<Texture2D>();
        for (var i = 0; ResourceLoader.Exists($"{SleepFolder}{prefix}_{i:00}.png"); i++)
            frames.Add(ResourceLoader.Load<Texture2D>($"{SleepFolder}{prefix}_{i:00}.png"));
        return frames;
    }

    private static string AbilityPath(CopyAbilityKind kind) =>
        kind == CopyAbilityKind.Void ? Void : AbilityPrefix + KirbyArt.SnakeCase(kind.ToString()) + ".png";

    private static void Set(Creature creature, string state, Variant value)
    {
        if (creature.Player?.Character is not Kirby || creature.GetCreatureNode()?.Body is not Sprite2D sprite)
            return;

        if (!sprite.HasMeta(BaseScaleMeta))
            sprite.SetMeta(BaseScaleMeta, sprite.Scale);
        var baseScale = sprite.GetMeta(BaseScaleMeta).AsVector2();
        sprite.SetMeta(state, value);
        if (sprite.GetMeta(SleepingMeta, false).AsBool())
            return;  // asleep (dead): states are remembered for a revive, the sleep animation stays

        var ability = sprite.GetMeta(AbilityMeta, "").AsString();
        var robobot = sprite.GetMeta(RobobotMeta, false).AsBool();
        var (path, scale) =
            sprite.GetMeta(SpittingMeta, 0).AsInt32() > 0 && ResourceLoader.Exists(Inhaling) ? (Inhaling, InhalingScale)
            : ability != "" && ResourceLoader.Exists(ability) ? (ability, sprite.GetMeta(AbilityScaleMeta, 1f).AsSingle())
            : !robobot && sprite.GetMeta(InhalingMeta, false).AsBool() && ResourceLoader.Exists(Inhaling) ? (Inhaling, InhalingScale)
            : sprite.GetMeta(MouthfulMeta, false).AsBool() && ResourceLoader.Exists(FullMouth) ? (FullMouth, FullMouthScale)
            : robobot && ResourceLoader.Exists(Robobot) ? (Robobot, RobobotScale)
            : (Normal, 1f);
        sprite.Texture = ResourceLoader.Load<Texture2D>(path);
        sprite.Scale = baseScale * scale;
    }
}
