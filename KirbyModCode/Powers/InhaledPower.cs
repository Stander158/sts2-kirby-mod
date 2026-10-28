using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// Inhaled enemies: before acting, they die if their HP is at or below <see cref="Threshold" />. Lasts until the
/// applier's next turn. The reward for the death is handled by Kirby's <see cref="InhalingPower" />. The threshold
/// is drawn on the enemy's health bar (see <see cref="Patches.InhaledHealthBarPatch" />).
/// </summary>
public abstract class InhaledPowerBase : ModPowerTemplate
{
    public abstract int Threshold { get; }

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>The highest threshold among the Inhaled powers on <paramref name="creature" />, or 0.</summary>
    public static int ThresholdOn(Creature creature) =>
        creature.Powers.OfType<InhaledPowerBase>().Select(power => power.Threshold).DefaultIfEmpty(0).Max();

    // AfterSideTurnStart, like Poison: killing a creature in BeforeSideTurnStart breaks the game's turn-start loop
    // (it still calls AfterTurnStart on the removed creature). Enemies have not acted yet at this point.
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side == Owner.Side)
        {
            if (Owner.IsAlive && Owner.CurrentHp <= Threshold)
            {
                Flash();
                await CreatureCmd.Kill(Owner);
            }
        }
        else if (Applier != null && side == Applier.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}

/// <summary>被吸入 (Inhale, Gooey Inhale, Scan): dies at 16 HP or less.</summary>
[RegisterPower]
public sealed class InhaledPower : InhaledPowerBase
{
    public override int Threshold => KirbyMouth.InhaleExecuteThreshold;

    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("devour_life_power");
}

/// <summary>被疯狂吸入 (Hypernova Inhale): dies at 33 HP or less.</summary>
[RegisterPower]
public sealed class FrenziedInhaledPower : InhaledPowerBase
{
    public const int FrenziedThreshold = 33;

    public override int Threshold => FrenziedThreshold;

    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("devour_life_power");
}
