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
            !card.DynamicVars.ContainsKey("Uses"))
            yield break;

        // RitsuLib formats the fragment with this card's dynamic vars, including upgrade previews.
        var text = new LocString("card_keywords", "CHEVAL_GRAND_SLAY_KEYWORD_STAMINA.cardText");
        text.Add("StaminaName", CGSKeywords.Stamina.GetModKeywordTitle());
        yield return new CardDescriptionFragment(text, CardDescriptionFragmentPlacement.AfterBase);
    }
}
