using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// From the Void card. At the start of your next turn, if you still hold the Void ability: lose 1 HP 60 times (true
/// HP loss: Block and Buffer do not stop it), each a slash with its sound, then lose every copy ability. Its tip only
/// says something garbled.
/// </summary>
[RegisterPower]
public sealed class VoidDoomPower : ModPowerTemplate
{
    public const int Hits = 60;

    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("devour_life_power");

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        if (Owner.HasPower<VoidPower>())
        {
            Flash();
            var defeat = Owner.HasPower<VoidDefeatPower>();
            for (var i = 0; i < Hits && Owner.IsAlive; i++)
            {
                await LoseHp(Owner, 1, i);
                if (!defeat)
                    continue;
                foreach (var enemy in Owner.CombatState?.HittableEnemies.ToList() ?? [])
                    await LoseHp(enemy, VoidDefeatPower.Damage, i);
            }

            if (Owner.IsAlive)
                await KirbyMouth.LoseCopyAbility(choiceContext, player);
        }

        await PowerCmd.Remove(Owner.GetPower<VoidDefeatPower>());
        await PowerCmd.Remove(this);
    }

    /// <summary>
    /// HP straight off, past Block, Buffer, Strength and Vulnerable, as a slash: the hurt animation every hit, the
    /// slash effect, its sound and a small shake every other one, so 60 of them stay quick.
    /// </summary>
    private static async Task LoseHp(Creature creature, int amount, int index)
    {
        if (!creature.IsAlive)
            return;

        if (index % 2 == 0)
        {
            VfxCmd.PlayOnCreatureCenter(creature, "vfx/vfx_attack_slash");
            SfxCmd.Play(KirbyFx.SlashSfx);
            NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
        }
        await CreatureCmd.TriggerAnim(creature, "Hit", 0f);
        await CreatureCmd.SetCurrentHp(creature, creature.CurrentHp - amount);
        await Cmd.Wait(0.03f);
    }
}

/// <summary>From the fifth Void card: while Void's 60 hits land, each one also takes 2 HP from every enemy (true HP loss).</summary>
[RegisterPower]
public sealed class VoidDefeatPower : ModPowerTemplate
{
    public const int Damage = 2;

    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("devour_life_power");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;
}

/// <summary>能力失去不能: Drop Ability (or the like) was tried while holding Void. Does nothing; gone next turn.</summary>
[RegisterPower]
public sealed class AbilityLockPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("barricade_power");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player)
            await PowerCmd.Remove(this);
    }
}
