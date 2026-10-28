using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace KirbyMod.Mechanics;

/// <summary>Hit effects shared by several attacks, borrowed from base-game cards.</summary>
internal static class KirbyFx
{
    /// <summary>Defect's Lightning orb evoke.</summary>
    public const string LightningSfx = "event:/sfx/characters/defect/defect_lightning_evoke";

    /// <summary>Burn, Fiend Fire.</summary>
    public const string FireSfx = "event:/sfx/characters/attack_fire";

    /// <summary>Plasma attacks: a lightning strike with its sound.</summary>
    public static AttackCommand WithLightningHit(this AttackCommand attack) =>
        attack.WithHitFx("vfx/vfx_attack_lightning", LightningSfx);

    /// <summary>Played Burns: the ground fire Burn shows at end of turn, with its sound.</summary>
    public static AttackCommand WithBurnHit(this AttackCommand attack) =>
        attack.WithHitFx(sfx: FireSfx).WithHitVfxNode(target => NGroundFireVfx.Create(target));
}
