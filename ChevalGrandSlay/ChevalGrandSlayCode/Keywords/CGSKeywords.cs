using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ChevalGrandSlay.Keywords;

[RegisterOwnedCardKeyword(StaminaStem)]
public sealed class CGSKeywords
{
    public const string StaminaStem = "stamina";

    public static CardKeyword Stamina => ModKeywordRegistry.GetCardKeyword(
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, StaminaStem));
}
