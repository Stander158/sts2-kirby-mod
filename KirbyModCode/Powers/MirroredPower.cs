using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// 镜之大迷宫 (on an enemy) for its next turn: each of its attack hits deals 0, and it gains that much Block instead.
/// </summary>
[RegisterPower]
public sealed class MirroredPower : ModPowerTemplate
{
    private sealed class Data
    {
        public bool AttackInProgress;
        public decimal PendingBaseDamage;
        public ValueProp PendingProps;
        public Creature? LastTarget;
    }

    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("reflect_power");

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    private Data State => GetInternalData<Data>();

    protected override object InitInternalData() => new Data();

    public override Task BeforeAttack(AttackCommand command)
    {
        if (command.Attacker != Owner || !command.DamageProps.IsPoweredAttack())
            return Task.CompletedTask;

        State.AttackInProgress = true;
        State.PendingProps = command.DamageProps;
        State.PendingBaseDamage = AttackMath.BaseDamage(command, null);
        return Task.CompletedTask;
    }

    public override Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (command.Attacker == Owner)
            State.AttackInProgress = false;
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!State.AttackInProgress || dealer != Owner || target == null || !props.IsPoweredAttack())
            return decimal.MaxValue;

        State.LastTarget = target;
        return 0m;
    }

    public override async Task AfterModifyingDamageAmount(CardModel? cardSource)
    {
        var state = State;
        if (!state.AttackInProgress || state.LastTarget == null)
            return;

        var block = AttackMath.UncappedDamage(state.LastTarget, Owner, state.PendingBaseDamage, state.PendingProps,
            cardSource);
        state.LastTarget = null;
        if (block <= 0)
            return;

        Flash();
        await CreatureCmd.GainBlock(Owner, block, ValueProp.Unpowered, null);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)
            await PowerCmd.Remove(this);
    }
}
