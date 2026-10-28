using KirbyMod.Cards;
using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>等离子: gain 1 energy when gained and at the start of each turn.</summary>
[RegisterPower]
public sealed class PlasmaPower : CopyAbilityPower
{
    public const int Energy = 1;

    public override CopyAbilityKind Kind => CopyAbilityKind.Plasma;

    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("high_voltage_power");

    public override Task OnGained(PlayerChoiceContext ctx, Player player) => PlayerCmd.GainEnergy(Energy, player);

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        Flash();
        await PlayerCmd.GainEnergy(Energy, player);
    }
}

/// <summary>汽车卡比: deal 13(18) damage to ALL enemies when gained, and 6 at the start of each turn.</summary>
[RegisterPower]
public sealed class CarPower : CopyAbilityPower
{
    public const int DamageOnGain = 13;
    public const int UpgradeBonus = 5;
    public const int DamagePerTurn = 6;

    public override CopyAbilityKind Kind => CopyAbilityKind.Car;

    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("speedster_power");

    public override Task OnGained(PlayerChoiceContext ctx, Player player) =>
        Ram(ctx, DamageOnGain + (Upgraded ? UpgradeBonus : 0));

    // Like Poison, turn-start damage goes in AfterSideTurnStart: killing earlier breaks the turn-start loop.
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side == Owner.Side)
            await Ram(new BlockingPlayerChoiceContext(), DamagePerTurn);
    }

    private async Task Ram(PlayerChoiceContext ctx, int damage)
    {
        var enemies = Owner.CombatState?.HittableEnemies.ToList();
        if (enemies == null || enemies.Count == 0)
            return;

        Flash();
        await CreatureCmd.Damage(ctx, enemies, damage, ValueProp.Unpowered, Owner, null);
    }
}

/// <summary>
/// 星之杖: gain 16 Mouthful when gained and 16 at the start of each turn, and a Spit Star+ in hand when gained.
/// While held, Spit Star keeps your Mouthful.
/// </summary>
[RegisterPower]
public sealed class StarRodPower : CopyAbilityPower
{
    public const int Mouthful = 16;
    public const int MouthfulPerTurn = 16;

    public override CopyAbilityKind Kind => CopyAbilityKind.StarRod;

    public override PowerAssetProfile AssetProfile => KirbyArt.Power("dream_rod");

    public override async Task OnGained(PlayerChoiceContext ctx, Player player)
    {
        await PowerCmd.Apply<MouthfulPower>(ctx, Owner, Mouthful, Owner, null);
        await KirbyMouth.AddToHandIfMissing<SpitStar>(player, upgraded: true);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        Flash();
        await PowerCmd.Apply<MouthfulPower>(choiceContext, Owner, MouthfulPerTurn, Owner, null);
    }
}

/// <summary>
/// 万能剑: gain 2 Strength when gained and at the start of each turn (all of it is lost with the ability). When
/// gained, add 2 random Sword cards to hand; they cost 0 this turn.
/// </summary>
[RegisterPower]
public sealed class GalaxiaPower : CopyAbilityPower
{
    public const int Strength = 2;
    public const int Cards = 2;

    public override CopyAbilityKind Kind => CopyAbilityKind.Galaxia;

    public override PowerAssetProfile AssetProfile => KirbyPowerIcons.Placeholder("sword_sage_power");

    public override async Task OnGained(PlayerChoiceContext ctx, Player player)
    {
        await Grant<StrengthPower>(ctx, Strength);

        var pool = CopyAbilities.CardsOf(CopyAbilityKind.Sword).ToList();
        var rng = player.RunState.Rng.CombatCardGeneration;
        for (var i = 0; i < Cards && pool.Count > 0; i++)
        {
            var type = rng.NextItem(pool)!;
            pool.Remove(type);
            var card = await CopyAbilities.GrantCard(player, type, Kind);
            card?.SetToFreeThisTurn();
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        Flash();
        await Grant<StrengthPower>(choiceContext, Strength);
    }
}
