#if !STS2_V113
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
#endif

namespace KirbyMod.Mechanics;

/// <summary>
/// Bridges game API differences between 0.107.1 and 0.113 (STS2_V113). Code is written against the 0.113 API;
/// on 0.107.1 these shims accept the extra arguments and call the old overloads.
/// </summary>
internal static class GameCompat
{
#if !STS2_V113
    /// <summary>0.113 added the CardPlay argument; 0.107.1's FromCard takes only the card.</summary>
    public static AttackCommand FromCard(this AttackCommand command, CardModel card, CardPlay? _) =>
        command.FromCard(card);
#endif
}
