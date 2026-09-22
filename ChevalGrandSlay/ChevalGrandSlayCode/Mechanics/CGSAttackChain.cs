using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ChevalGrandSlay.Mechanics;

internal static class CGSAttackChain
{
    public static int CountAttacksPlayedThisTurn(
        Player? player,
        ICombatState? combatState,
        CardPlay? excludedPlay = null)
    {
        if (player == null || combatState == null || !CombatManager.Instance.IsInProgress)
            return 0;

        return CombatManager.Instance.History.CardPlaysStarted.Count(entry =>
            entry.HappenedThisTurn(combatState) &&
            entry.CardPlay.Card.Owner == player &&
            entry.CardPlay.Card.Type == CardType.Attack &&
            !ReferenceEquals(entry.CardPlay, excludedPlay));
    }
}