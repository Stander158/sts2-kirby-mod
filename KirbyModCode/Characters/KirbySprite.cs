using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace KirbyMod.Characters;

/// <summary>
/// Swaps Kirby's combat sprite to the full-mouth picture, shown twice as big, while he has Mouthful (if that picture
/// is present). The sprite is anchored at Kirby's feet, so he grows upward.
/// </summary>
internal static class KirbySprite
{
    private const string Normal = KirbyTheme.CharacterImages + "kirby_combat.png";
    private const string FullMouth = KirbyTheme.CharacterImages + "kirby_mouthful.png";
    private const float FullMouthScale = 2f;
    private const string BaseScaleMeta = "kirby_base_scale";

    public static void SetMouthful(Creature creature, bool full)
    {
        if (creature.Player?.Character is not Kirby || creature.GetCreatureNode()?.Body is not Sprite2D sprite)
            return;

        if (!sprite.HasMeta(BaseScaleMeta))
            sprite.SetMeta(BaseScaleMeta, sprite.Scale);
        var baseScale = sprite.GetMeta(BaseScaleMeta).AsVector2();

        full = full && ResourceLoader.Exists(FullMouth);
        sprite.Texture = ResourceLoader.Load<Texture2D>(full ? FullMouth : Normal);
        sprite.Scale = full ? baseScale * FullMouthScale : baseScale;
    }
}
