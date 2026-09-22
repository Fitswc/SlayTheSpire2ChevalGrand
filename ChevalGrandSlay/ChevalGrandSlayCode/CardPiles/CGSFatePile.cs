using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ChevalGrandSlay.CardPiles;

[RegisterOwnedCardPile(LocalId,
    Style = ModCardPileUiStyle.BottomRight,
    AnchorKind = ModCardPileAnchorKind.BottomRightSecondary)]
public sealed class CGSFatePile
{
    public const string LocalId = "Fate";

    public static string Id => ModContentRegistry.GetQualifiedCardPileId(Entry.ModId, LocalId);
    public static PileType PileType => Id.GetModCardPileType();
}