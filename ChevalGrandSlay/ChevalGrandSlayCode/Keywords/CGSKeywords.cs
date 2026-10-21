using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ChevalGrandSlay.Keywords;

[RegisterOwnedCardKeyword(StaminaStem,
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None,
    IncludeInCardHoverTip = true)]
[RegisterOwnedCardKeyword(RestPileStem,
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None,
    IncludeInCardHoverTip = true)]
public sealed class CGSKeywords
{
    public const string StaminaStem = "stamina";
    public const string RestPileStem = "rest_pile";

    public static CardKeyword Stamina => ModKeywordRegistry.GetCardKeyword(
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, StaminaStem));

    public static CardKeyword RestPile => ModKeywordRegistry.GetCardKeyword(
        ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, RestPileStem));
}
