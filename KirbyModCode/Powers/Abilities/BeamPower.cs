using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// 光束: before an enemy with Clock attacks you, gain Block equal to its Clock points (once per attack, so a multi-hit
/// attack gives it once; all / random attacks on the players' side count). 2026-11-18; it used to give ALL enemies
/// 1 Clock each turn, and briefly 1 extra draw a turn. V-O-I-D counts as Beam but does not do this.
/// </summary>
[RegisterPower]
public sealed class BeamPower : CopyAbilityPower
{
    public override CopyAbilityKind Kind => CopyAbilityKind.Beam;

    public override PowerAssetProfile AssetProfile => KirbyArt.Power("clock");

    public override async Task BeforeAttack(AttackCommand command)
    {
        if (command.Attacker is not { } enemy || enemy.Side == Owner.Side || !Owner.IsAlive
            || !command.DamageProps.IsPoweredAttack() || !AttackMath.AimsAt(command, Owner))
            return;

        var block = ClockPower.PointsOf(enemy);
        if (block <= 0)
            return;

        Flash();
        await CreatureCmd.GainBlock(Owner, block, ValueProp.Unpowered, null);
    }
}
