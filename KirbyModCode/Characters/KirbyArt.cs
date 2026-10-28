using KirbyMod.Cards;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Characters;

/// <summary>
/// Which picture each card, power and relic uses. The art comes from KindAiden's "CustomCharacterKirby" mod
/// (personal use only, see DESIGN.md) and is not in the repository: a build without it (see AssembleArt in the
/// .csproj) falls back to RitsuLib's placeholder / a base-game icon, as does anything not listed.
/// </summary>
internal static class KirbyArt
{
    private const string Images = "res://KirbyMod/images/";

    private static readonly Dictionary<Type, string> CardPortraits = new()
    {
        [typeof(Inhale)] = "inhale",
        [typeof(GooeyInhale)] = "dimensional_rift",
        [typeof(SpitStar)] = "star_spit",
        [typeof(Swallow)] = "devour",
        [typeof(SwordAbility)] = "sword_essence",
        [typeof(IceAbility)] = "ice_essence",
        [typeof(SleepAbility)] = "sleep_essence",
        [typeof(FighterAbility)] = "guard",
        [typeof(GalaxyWish)] = "galactic_nova",
        [typeof(DededeInhale)] = "hypernova",
        [typeof(UltimateFruit)] = "gem_apple",
        [typeof(FountainOfDreams)] = "dream_rod",
        [typeof(MirrorLabyrinth)] = "dimension_mirror",
        [typeof(ReturnToDreamLand)] = "warp_star",
        [typeof(SpringBreeze)] = "hover",
        [typeof(AbilityShield)] = "prism_shield",
        [typeof(MetaKnightAid)] = "call_for_help",
        [typeof(SuperAbility)] = "ultra_sword",
        [typeof(MineBattle)] = "treasure_chest",
        [typeof(StarBulletBoost)] = "energy_sphere",
        [typeof(BubbleStorage)] = "strawberry_shortcake",
        [typeof(SmashBrosKirby)] = "hal_room",
        // 波动拳: the old "crackler" (cannon Kirby) was the wrong picture; placeholder until Fighter art is found.
        [typeof(IceBreath)] = "crouch",
        [typeof(InvincibleGuard)] = "sun_stone",
        [typeof(MixAbility)] = "action_star",
        [typeof(Robobot)] = "robobot_armor",
        [typeof(Scan)] = "code_cube",
        [typeof(StarRodAbility)] = "dream_rod",
        [typeof(GalaxiaAbility)] = "sky_energy_sword",
        [typeof(CarKirby)] = "slide",
        [typeof(UltimateInhale)] = "ultimate_inhale",
    };

    private static bool Exists(string path) => Godot.ResourceLoader.Exists(path);

    public static CardAssetProfile ForCard(Type cardType) =>
        CardPortraits.TryGetValue(cardType, out var name) && Exists($"{Images}cards/{name}.png")
            ? new CardAssetProfile(PortraitPath: $"{Images}cards/{name}.png")
            : CardAssetProfile.Empty;

    public static PowerAssetProfile Power(string name) =>
        Exists($"{Images}powers/{name}.png")
            ? new($"{Images}powers/{name}.png", $"{Images}powers/big/{name}.png")
            : Powers.KirbyPowerIcons.Placeholder("buffer_power");

    /// <summary>A base-game relic's icon, until Kirby's own relic art is drawn.</summary>
    public static RelicAssetProfile VanillaRelic(string name) => new(
        $"res://images/atlases/relic_atlas.sprites/{name}.tres",
        $"res://images/atlases/relic_outline_atlas.sprites/{name}.tres",
        $"res://images/relics/{name}.png");

    public static RelicAssetProfile Relic(string name) =>
        Exists($"{Images}relics/{name}.png")
            ? new($"{Images}relics/{name}.png", $"{Images}relics/{name}_outline.png", $"{Images}relics/big/{name}.png")
            : VanillaRelic("anchor");
}
