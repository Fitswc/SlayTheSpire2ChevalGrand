using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models.Capabilities;

namespace ChevalGrandSlay.Keywords;

[RegisterModelCapability]
public sealed class CGSStaminaKeywordText : ModelCapability, ICardDescriptionContributor
{
    public IEnumerable<CardDescriptionFragment> GetDescriptionFragments(CardDescriptionContext context)
    {
        if (Owner is not CardModel card || !card.Keywords.Contains(CGSKeywords.Stamina) ||
            !CGSStamina.HasStamina(card))
            yield break;

        var text = new LocString("card_keywords", "CHEVAL_GRAND_SLAY_KEYWORD_STAMINA.cardText");
        if (card.Pile?.Type == PileType.Deck)
        {
            decimal remaining = CGSStamina.GetDeckStamina(card);
            if (remaining < card.DynamicVars[CGSStamina.Uses].BaseValue)
            {
                text = new LocString("card_keywords", "CHEVAL_GRAND_SLAY_KEYWORD_STAMINA.deckText");
                text.Add("RemainingStamina", remaining);
            }
        }
        text.Add("StaminaName", CGSKeywords.Stamina.GetModKeywordTitle());
        yield return new CardDescriptionFragment(text);
    }
}
