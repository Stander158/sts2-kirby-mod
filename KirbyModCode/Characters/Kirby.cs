using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;

namespace KirbyMod.Characters;

/// <summary>
/// Kirby. Character art is taken from KindAiden's "CustomCharacterKirby" mod (personal use only, see DESIGN.md);
/// anything not set here (multiplayer hands, card trail, sounds) still falls back to the Ironclad.
/// </summary>
[RegisterCharacter]
public sealed class Kirby : ModCharacterTemplate<KirbyCardPool, KirbyRelicPool, KirbyPotionPool>
{
    private const string VisualsScene = KirbyTheme.Scenes + "kirby_visuals.tscn";

    public override CharacterAssetProfile AssetProfile => new(
        Scenes: new CharacterSceneAssetSet(
            VisualsPath: VisualsScene,
            EnergyCounterPath: KirbyTheme.Scenes + "kirby_energy_counter.tscn",
            MerchantAnimPath: KirbyTheme.Scenes + "kirby_merchant.tscn",
            RestSiteAnimPath: KirbyTheme.Scenes + "kirby_rest.tscn"),
        Ui: new CharacterUiAssetSet(
            IconTexturePath: KirbyTheme.CharacterImages + "icon.png",
            IconPath: KirbyTheme.Scenes + "kirby_icon.tscn",
            IconOutlineTexturePath: KirbyTheme.CharacterImages + "icon.png",
            CharacterSelectBgPath: KirbyTheme.Scenes + "kirby_select_bg.tscn",
            CharacterSelectIconPath: KirbyTheme.CharacterImages + "char_select.png",
            CharacterSelectLockedIconPath: KirbyTheme.CharacterImages + "char_select_locked.png",
            MapMarkerPath: KirbyTheme.CharacterImages + "map_marker.png"));

    public override string? PlaceholderCharacterId => "ironclad";

    public override Color NameColor => KirbyTheme.Pink;

    public override Color EnergyLabelOutlineColor => KirbyTheme.PinkDark;

    public override Color MapDrawingColor => KirbyTheme.Pink;

    public override CharacterGender Gender => CharacterGender.Masculine;

    public override int StartingHp => 72;

    public override int StartingGold => 99;

    public override bool RequiresEpochAndTimeline => false;

    public override float AttackAnimDelay => 0.15f;

    public override float CastAnimDelay => 0.25f;

    public override List<string> GetArchitectAttackVfx() =>
        ["vfx/vfx_attack_blunt", "vfx/vfx_attack_slash", "vfx/vfx_heavy_blunt"];

    protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
        RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(VisualsScene);
}
