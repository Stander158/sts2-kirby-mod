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
/// <see cref="KirbyMouth.DropCopyAbility" />); <see cref="VoidDoomPower" /> ends it.
/// </summary>
[RegisterPower]
public sealed class VoidPower : CopyAbilityPower
{
    public override CopyAbilityKind Kind => CopyAbilityKind.Void;

    public override IReadOnlyList<CopyAbilityKind> CountsAs =>
    [
        CopyAbilityKind.Void, CopyAbilityKind.Sword, CopyAbilityKind.Boomerang, CopyAbilityKind.Stone,
        CopyAbilityKind.Fighter, CopyAbilityKind.Ice, CopyAbilityKind.Fire, CopyAbilityKind.Plasma,
        CopyAbilityKind.Doctor, CopyAbilityKind.Wheel,
    ];

    public override bool LostToHeavyHits => false;

    public override PowerAssetProfile AssetProfile => KirbyArt.Power("void");

    public override async Task OnGained(PlayerChoiceContext ctx, Player player)
    {
        await Grant<StrengthPower>(ctx, SwordPower.Strength + BoomerangPower.Strength + FighterPower.Strength);
        await Grant<DexterityPower>(ctx, BoomerangPower.Dexterity + WheelPower.Dexterity);
        await Grant<VigorPower>(ctx, SwordPower.VigorOnGain);
        await Grant<BufferPower>(ctx, StonePower.BufferOnGain);
        await CreatureCmd.GainBlock(Owner, WheelPower.BlockOnGain, ValueProp.Unpowered, null);
    }

    // Counts as Sword and Doctor: Meta Knight's Revenge costs 0, Experiment 1 less (see SwordPower, DoctorPower).
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner != Owner.Player)
            return false;

        if (card is MetaKnightRevenge)
            modifiedCost = 0m;
        else if (card is Experiment)
            modifiedCost = Math.Max(0m, originalCost - 1);
        else
            return false;
        return true;
    }
}
