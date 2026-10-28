using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>泡泡储存: Swallow no longer clears Mouthful.</summary>
[RegisterPower]
public sealed class BubbleStoragePower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("blur_power");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;
}

/// <summary>超级能力: each of the next N copy abilities is gained twice (see <see cref="Mechanics.CopyAbilities.Gain" />).</summary>
[RegisterPower]
public sealed class SuperAbilityPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("demon_form_power");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}

/// <summary>大乱斗卡比: copy abilities coexist; gaining one no longer removes the others.</summary>
[RegisterPower]
public sealed class SmashBrosPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("thorns_power");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;
}

/// <summary>星星弹强化: Spit Star deals +Amount% damage; a Spit Star of 20+ damage also hits every other enemy.</summary>
[RegisterPower]
public sealed class StarBulletPower : ModPowerTemplate
{
    public const int SplashThreshold = 20;

    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("flame_barrier_power");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
