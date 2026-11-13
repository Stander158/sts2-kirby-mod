using KirbyMod.Cards;
using KirbyMod.Characters;
using KirbyMod.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace KirbyMod.Powers;

/// <summary>
/// 虚无 (from the Void card): every ability at once, folded into one. It keeps the stat bonuses of the abilities it
/// soaked up (none of their cards, energy, Burns or Ice Blocks; Sleep, Carby, Star Rod and Master are left out) and
/// counts as each of them for "X: ..." cards. It cannot be lost to damage or dropped (see
/// <see cref="KirbyMouth.DropCopyAbility" />) and no other ability can be gained on top of it (see
/// <see cref="CopyAbilities.Gain" />); <see cref="VoidDoomPower" /> ends it. While held, Kirby's sprite is Void.
/// When a new copy ability is added to the mod, add its on-gain stat bonus to <see cref="OnGained" /> and its kind to
/// <see cref="CountsAs" /> (and update the V-O-I-D texts and DESIGN.md).
/// </summary>
[RegisterPower]
public sealed class VoidPower : CopyAbilityPower
{
    public override CopyAbilityKind Kind => CopyAbilityKind.Void;

    public override IReadOnlyList<CopyAbilityKind> CountsAs =>
    [
        CopyAbilityKind.Void, CopyAbilityKind.Sword, CopyAbilityKind.Boomerang, CopyAbilityKind.Stone,
        CopyAbilityKind.Fighter, CopyAbilityKind.Ice, CopyAbilityKind.Fire, CopyAbilityKind.Plasma,
        CopyAbilityKind.Doctor, CopyAbilityKind.Wheel, CopyAbilityKind.Beam,
    ];

    public override bool LostToHeavyHits => false;

    public override PowerAssetProfile AssetProfile => KirbyArt.Power("void");

    public override async Task OnGained(PlayerChoiceContext ctx, Player player)
    {
        var k = GainMultiplier;
        await Grant<StrengthPower>(ctx, (SwordPower.Strength + BoomerangPower.Strength + FighterPower.Strength) * k);
        await Grant<DexterityPower>(ctx, (BoomerangPower.Dexterity + WheelPower.Dexterity) * k);
        await Grant<VigorPower>(ctx, SwordPower.VigorOnGain * k);
        await Grant<BufferPower>(ctx, StonePower.BufferOnGain * k);
        await CreatureCmd.GainBlock(Owner, WheelPower.BlockOnGain * k, ValueProp.Unpowered, null);
    }

    // Counts as Sword and Doctor: Meta Knight's Revenge costs 1, Experiment 1 less (see SwordPower, DoctorPower).
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner != Owner.Player)
            return false;

        if (card is MetaKnightRevenge)
            modifiedCost = Math.Min(originalCost, MetaKnightRevenge.SwordCost);
        else if (card is Experiment)
            modifiedCost = Math.Max(0m, originalCost - 1);
        else
            return false;
        return true;
    }
}
