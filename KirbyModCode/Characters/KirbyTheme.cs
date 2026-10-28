using Godot;

namespace KirbyMod.Characters;

/// <summary>Shared colors and art paths for Kirby.</summary>
public static class KirbyTheme
{
    public static readonly Color Pink = new(1f, 0.53f, 0.71f);
    public static readonly Color PinkDark = new(0.62f, 0.16f, 0.38f);

    public const string EnergyColorName = "kirbymod";

    public const string CharacterImages = "res://KirbyMod/images/character/";
    public const string Scenes = "res://KirbyMod/scenes/";

    public const string BigEnergyIcon = CharacterImages + "energy_big.png";
    public const string TextEnergyIcon = CharacterImages + "energy_text.png";
}
