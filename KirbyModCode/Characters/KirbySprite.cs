using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace KirbyMod.Characters;

/// <summary>
/// Kirby's combat sprite: Void (huge) while he holds the Void ability, else the full-mouth picture (twice as big) while
/// he has Mouthful, else the normal one; a picture that is not there (public build) falls back to the normal sprite.
/// The sprite is anchored at Kirby's feet, so he grows upward. Each state is remembered on the sprite node.
/// </summary>
internal static class KirbySprite
{
    private const string Normal = KirbyTheme.CharacterImages + "kirby_combat.png";
    private const string FullMouth = KirbyTheme.CharacterImages + "kirby_mouthful.png";
    private const string Void = KirbyTheme.CharacterImages + "kirby_void.png";
    private const float FullMouthScale = 2f;
    private const float VoidScale = 2.6f;
    private const string BaseScaleMeta = "kirby_base_scale";
    private const string MouthfulMeta = "kirby_mouthful";
    private const string VoidMeta = "kirby_void";

    public static void SetMouthful(Creature creature, bool full) => Set(creature, MouthfulMeta, full);

    public static void SetVoid(Creature creature, bool isVoid) => Set(creature, VoidMeta, isVoid);

    private static void Set(Creature creature, string state, bool on)
    {
        if (creature.Player?.Character is not Kirby || creature.GetCreatureNode()?.Body is not Sprite2D sprite)
            return;

        if (!sprite.HasMeta(BaseScaleMeta))
            sprite.SetMeta(BaseScaleMeta, sprite.Scale);
        var baseScale = sprite.GetMeta(BaseScaleMeta).AsVector2();
        sprite.SetMeta(state, on);

        var (path, scale) =
            Is(sprite, VoidMeta) && ResourceLoader.Exists(Void) ? (Void, VoidScale)
            : Is(sprite, MouthfulMeta) && ResourceLoader.Exists(FullMouth) ? (FullMouth, FullMouthScale)
            : (Normal, 1f);
        sprite.Texture = ResourceLoader.Load<Texture2D>(path);
        sprite.Scale = baseScale * scale;
    }

    private static bool Is(Sprite2D sprite, string state) => sprite.HasMeta(state) && sprite.GetMeta(state).AsBool();
}
