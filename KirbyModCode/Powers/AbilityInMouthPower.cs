using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// 复制能力入嘴: remembers which copy ability Swallow will grant. Its title and text are per ability,
/// e.g. <c>KIRBY_MOD_POWER_ABILITY_IN_MOUTH_POWER.SWORD.title</c>.
/// </summary>
[RegisterPower]
public sealed class AbilityInMouthPower : ModPowerTemplate
{
    private sealed class Data
    {
        public CopyAbilityKind Kind;
    }

    public override PowerAssetProfile AssetProfile => KirbyArt.Power("normal_ability");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public CopyAbilityKind Kind
    {
        // The canonical (non-combat) instance has no internal data.
        get => GetInternalData<Data?>()?.Kind ?? CopyAbilityKind.Sword;
        set => GetInternalData<Data>().Kind = value;
    }

    private string KindKey => $"{Id.Entry}.{Kind.ToString().ToUpperInvariant()}";

    public override LocString Title => new("powers", KindKey + ".title");

    public override LocString Description => new("powers", KindKey + ".description");

    protected override string SmartDescriptionLocKey => KindKey + ".description";

    protected override object InitInternalData() => new Data();
}
