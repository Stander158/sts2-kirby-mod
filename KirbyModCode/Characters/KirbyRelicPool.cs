using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Characters;

public sealed class KirbyRelicPool : TypeListRelicPoolModel
{
    public override string EnergyColorName => KirbyTheme.EnergyColorName;

    public override Color LabOutlineColor => KirbyTheme.Pink;

    public override string? BigEnergyIconPath => KirbyTheme.BigEnergyIcon;

    public override string? TextEnergyIconPath => KirbyTheme.TextEnergyIcon;
}
