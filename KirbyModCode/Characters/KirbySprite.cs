using Godot;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace KirbyMod.Characters;

/// <summary>
/// Kirby's combat sprite, in order of priority: the first-gained copy ability he holds (Void huge), Mouthful (twice as
/// big), the Robobot Armor, else the normal one. A picture that is not there (public build) is skipped. The sprite is
/// anchored at Kirby's feet, so bigger ones grow upward. Each state is remembered on the sprite node.
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

    public static void SetMouthful(Creature creature, bool full) => Set(creature, MouthfulMeta, full);

    public static void SetRobobot(Creature creature, bool on) => Set(creature, RobobotMeta, on);

    /// <summary><paramref name="kind" />: the ability that now decides the sprite, or null for none.</summary>
    public static void SetAbility(Creature creature, CopyAbilityKind? kind) =>
        Set(creature, AbilityMeta, kind == null ? "" : AbilityPath(kind.Value));

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
            ability != "" && ResourceLoader.Exists(ability) ? (ability, ability == Void ? VoidScale : 1f)
            : sprite.GetMeta(MouthfulMeta, false).AsBool() && ResourceLoader.Exists(FullMouth) ? (FullMouth, FullMouthScale)
            : sprite.GetMeta(RobobotMeta, false).AsBool() && ResourceLoader.Exists(Robobot) ? (Robobot, RobobotScale)
            : (Normal, 1f);
        sprite.Texture = ResourceLoader.Load<Texture2D>(path);
        sprite.Scale = baseScale * scale;
    }
}
