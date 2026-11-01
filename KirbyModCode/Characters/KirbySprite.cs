using Godot;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace KirbyMod.Characters;

/// <summary>
/// Kirby's combat sprite, in order of priority: the first-gained copy ability he holds (Void huge), Mouthful (twice as
/// big), the Robobot Armor, else the normal one. A picture that is not there (public build) is skipped. Ability pictures
/// fill the same canvas whatever else is in them (a long sword, a wheel...), so each has its own scale that brings
/// Kirby's body to the size of the normal sprite (Carby is simply big). The sprite is anchored at Kirby's feet, so bigger
/// ones grow upward. Each state is remembered on the sprite node.
/// </summary>
internal static class KirbySprite
{
    private const string Normal = KirbyTheme.CharacterImages + "kirby_combat.png";
    private const string FullMouth = KirbyTheme.CharacterImages + "kirby_mouthful.png";
    private const string Robobot = KirbyTheme.CharacterImages + "kirby_robobot.png";
    private const string Void = KirbyTheme.CharacterImages + "kirby_void.png";
    private const string AbilityPrefix = KirbyTheme.CharacterImages + "kirby_ability_";
    private const float FullMouthScale = 2f;
    private const float RobobotScale = 1.5f;
    private const float VoidScale = 2.6f;
    private const string BaseScaleMeta = "kirby_base_scale";
    private const string MouthfulMeta = "kirby_mouthful";
    private const string RobobotMeta = "kirby_robobot";
    private const string AbilityMeta = "kirby_ability";
    private const string AbilityScaleMeta = "kirby_ability_scale";

    private static readonly Dictionary<CopyAbilityKind, float> AbilityScales = new()
    {
        [CopyAbilityKind.Sword] = 1.5f,
        [CopyAbilityKind.Boomerang] = 1.25f,
        [CopyAbilityKind.Stone] = 1.9f,
        [CopyAbilityKind.Fighter] = 1.5f,
        [CopyAbilityKind.Ice] = 1.45f,
        [CopyAbilityKind.Fire] = 2f,
        [CopyAbilityKind.Sleep] = 1.2f,
        [CopyAbilityKind.Plasma] = 2.4f,
        [CopyAbilityKind.Car] = 2.2f,
        [CopyAbilityKind.StarRod] = 2.6f,
        [CopyAbilityKind.Galaxia] = 2.4f,
        [CopyAbilityKind.Doctor] = 2f,
        [CopyAbilityKind.Wheel] = 1.2f,
        [CopyAbilityKind.Void] = VoidScale,
    };

    public static void SetMouthful(Creature creature, bool full) => Set(creature, MouthfulMeta, full);

    public static void SetRobobot(Creature creature, bool on) => Set(creature, RobobotMeta, on);

    /// <summary><paramref name="kind" />: the ability that now decides the sprite, or null for none.</summary>
    public static void SetAbility(Creature creature, CopyAbilityKind? kind)
    {
        if (creature.GetCreatureNode()?.Body is Sprite2D sprite)
            sprite.SetMeta(AbilityScaleMeta, kind == null ? 1f : AbilityScales.GetValueOrDefault(kind.Value, 1f));
        Set(creature, AbilityMeta, kind == null ? "" : AbilityPath(kind.Value));
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

        var ability = sprite.GetMeta(AbilityMeta, "").AsString();
        var (path, scale) =
            ability != "" && ResourceLoader.Exists(ability) ? (ability, sprite.GetMeta(AbilityScaleMeta, 1f).AsSingle())
            : sprite.GetMeta(MouthfulMeta, false).AsBool() && ResourceLoader.Exists(FullMouth) ? (FullMouth, FullMouthScale)
            : sprite.GetMeta(RobobotMeta, false).AsBool() && ResourceLoader.Exists(Robobot) ? (Robobot, RobobotScale)
            : (Normal, 1f);
        sprite.Texture = ResourceLoader.Load<Texture2D>(path);
        sprite.Scale = baseScale * scale;
    }
}
