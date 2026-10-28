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
        // Kept from the first art pass.
        [typeof(MixAbility)] = "action_star",
        [typeof(StarBulletBoost)] = "energy_sphere",
        [typeof(SuperAbility)] = "ultra_sword",
        [typeof(Robobot)] = "robobot_armor",
        [typeof(GalaxyWish)] = "galactic_nova",
        [typeof(UltimateInhale)] = "ultimate_inhale",
        [typeof(VoidCard)] = "void_card",

        // Official artwork picked from WiKirby (art-sources/wikirby/card_art_map.py, outside the repository).
        [typeof(SwordAbility)] = "wk_sword_ability",
        [typeof(BoomerangAbility)] = "wk_boomerang_ability",
        [typeof(StoneAbility)] = "wk_stone_ability",
        [typeof(FighterAbility)] = "wk_fighter_ability",
        [typeof(IceAbility)] = "wk_ice_ability",
        [typeof(FireAbility)] = "wk_fire_ability",
        [typeof(SleepAbility)] = "wk_sleep_ability",
        [typeof(PlasmaAbility)] = "wk_plasma_ability",
        [typeof(CarKirby)] = "wk_car_kirby",
        [typeof(StarRodAbility)] = "wk_star_rod_ability",
        [typeof(GalaxiaAbility)] = "wk_galaxia_ability",
        [typeof(DoctorAbility)] = "wk_doctor_ability",
        [typeof(DropAbility)] = "wk_drop_ability",
        [typeof(Inhale)] = "wk_inhale",
        [typeof(KirbyDefend)] = "wk_kirby_defend",
        [typeof(KirbyStrike)] = "wk_kirby_strike",
        [typeof(SpitStar)] = "wk_spit_star",
        [typeof(Swallow)] = "wk_swallow",
        [typeof(GooeyInhale)] = "wk_gooey_inhale",
        [typeof(BubbleStorage)] = "wk_bubble_storage",
        [typeof(DededeInhale)] = "wk_dedede_inhale",
        [typeof(Scan)] = "wk_scan",
        [typeof(AbilityShield)] = "wk_ability_shield",
        [typeof(UltimateFruit)] = "wk_ultimate_fruit",
        [typeof(ReturnToDreamLand)] = "wk_return_to_dream_land",
        [typeof(SmashBrosKirby)] = "wk_smash_bros_kirby",
        [typeof(AbilityOffering)] = "wk_ability_offering",
        [typeof(Feed)] = "wk_feed",
        [typeof(MineBattle)] = "wk_mine_battle",
        [typeof(SpringBreeze)] = "wk_spring_breeze",
        [typeof(SteelWing)] = "wk_steel_wing",
        [typeof(FountainOfDreams)] = "wk_fountain_of_dreams",
        [typeof(MirrorLabyrinth)] = "wk_mirror_labyrinth",
        [typeof(InvincibleLollipop)] = "wk_invincible_lollipop",
        [typeof(PinkDemon)] = "wk_pink_demon",
        [typeof(Slash)] = "wk_slash",
        [typeof(SpinSlash)] = "wk_spin_slash",
        [typeof(RisingSlash)] = "wk_rising_slash",
        [typeof(DescendingSlash)] = "wk_descending_slash",
        [typeof(MachTornado)] = "wk_mach_tornado",
        [typeof(UltraSword)] = "wk_ultra_sword",
        [typeof(MetaKnightAid)] = "wk_meta_knight_aid",
        [typeof(MetaKnightRevenge)] = "wk_meta_knight_revenge",
        [typeof(MetaNightmare)] = "wk_meta_nightmare",
        [typeof(BoomerangThrow)] = "wk_boomerang_throw",
        [typeof(FinalCutter)] = "wk_final_cutter",
        [typeof(InvincibleGuard)] = "wk_invincible_guard",
        [typeof(StonePunch)] = "wk_stone_punch",
        [typeof(CurlingStone)] = "wk_curling_stone",
        [typeof(ChargeUp)] = "wk_charge_up",
        [typeof(ForceBlast)] = "wk_force_blast",
        [typeof(Launch)] = "wk_launch",
        [typeof(VacuumStrike)] = "wk_vacuum_strike",
        [typeof(IceBreath)] = "wk_ice_breath",
        [typeof(IceBlock)] = "wk_ice_block",
        [typeof(Frost)] = "wk_frost",
        [typeof(SnowBowl)] = "wk_snow_bowl",
        [typeof(KirbyFireball)] = "wk_kirby_fireball",
        [typeof(DragonStorm)] = "wk_dragon_storm",
        [typeof(PlasmaCannon)] = "wk_plasma_cannon",
        [typeof(PlasmaCharge)] = "wk_plasma_charge",
        [typeof(UltimateFlash)] = "wk_ultimate_flash",
        [typeof(PillThrow)] = "wk_pill_throw",
        [typeof(MakeMedicine)] = "wk_make_medicine",
        [typeof(Experiment)] = "wk_experiment",
        [typeof(Dodge)] = "wk_dodge",
        [typeof(WheelDash)] = "wk_wheel_dash",
        [typeof(WheelCharge)] = "wk_wheel_charge",
        [typeof(WheelieRider)] = "wk_wheelie_rider",
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
