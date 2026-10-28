using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>Borrowed base-game power icons until Kirby's own power art is drawn.</summary>
internal static class KirbyPowerIcons
{
    public static PowerAssetProfile Placeholder(string vanillaPowerEntry) => new(
        $"res://images/atlases/power_atlas.sprites/{vanillaPowerEntry}.tres",
        $"res://images/powers/{vanillaPowerEntry}.png");
}
