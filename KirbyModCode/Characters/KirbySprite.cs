using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace KirbyMod.Characters;

/// <summary>Swaps Kirby's combat sprite to the full-mouth picture while he has Mouthful (if that picture is present).</summary>
internal static class KirbySprite
{
    private const string Normal = KirbyTheme.CharacterImages + "kirby_combat.png";
    private const string FullMouth = KirbyTheme.CharacterImages + "kirby_mouthful.png";

    public static void SetMouthful(Creature creature, bool full)
    {
        if (creature.Player?.Character is not Kirby || creature.GetCreatureNode()?.Body is not Sprite2D sprite)
            return;

        var path = full && ResourceLoader.Exists(FullMouth) ? FullMouth : Normal;
        sprite.Texture = ResourceLoader.Load<Texture2D>(path);
    }
}
