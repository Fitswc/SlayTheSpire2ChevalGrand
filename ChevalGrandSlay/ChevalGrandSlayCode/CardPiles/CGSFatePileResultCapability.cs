using ChevalGrandSlay.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace ChevalGrandSlay.CardPiles;

[RegisterModelCapability]
public sealed class CGSFatePileResultCapability : CardCapability, ICardPlayResultContributor
{
    public PileType? GetResultPileTypeForCardPlay(CardModel card)
    {
        return card is CGSLimitedUseCard { RemainingUses: 1 }
            ? CGSFatePile.PileType
            : null;
    }
}