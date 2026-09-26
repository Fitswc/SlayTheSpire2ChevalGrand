using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ChevalGrandSlay.Mechanics;

internal static class CGSLimitedCardTypeAndCount
{
    public static bool IsAttack(CardModel card) =>
        card is CGSLimitedUseCard { RemainingUses: > 0 } &&
        card.DeckVersion is not null && card.Type == CardType.Attack;

    public static bool IsSkill(CardModel card) =>
        card is CGSLimitedUseCard { RemainingUses: > 0 } &&
        card.DeckVersion is not null && card.Type == CardType.Skill;

    public static bool IsDefensiveSkill(CardModel card) =>
        IsSkill(card) && (card.Tags.Contains(CardTag.Defend) || card.DynamicVars.ContainsKey("Block"));
}
