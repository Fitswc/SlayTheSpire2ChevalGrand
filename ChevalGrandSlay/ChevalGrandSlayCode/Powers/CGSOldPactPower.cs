using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSOldPactPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => new();

    private bool _usedThisTurn;
    private CardModel? _preservedCard;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player)
        {
            _usedThisTurn = false;
            _preservedCard = null;
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card == _preservedCard) _preservedCard = null;
        return Task.CompletedTask;
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal cost, out decimal modified)
    {
        modified = cost;
        if (_usedThisTurn || !IsOriginalLimitedCard(card)) return false;
        modified = Math.Max(0m, cost - 1m);
        return modified != cost;
    }

    public bool TryPreserveUse(CardPlay cardPlay)
    {
        return !cardPlay.IsAutoPlay && cardPlay.Card == _preservedCard;
    }

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card, bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position)
    {
        if (_usedThisTurn || isAutoPlay || !IsOriginalLimitedCard(card))
            return (pileType, position);
        _usedThisTurn = true;
        _preservedCard = card;
        return (PileType.Discard, position);
    }

    private bool IsOriginalLimitedCard(CardModel card) =>
        card is CGSLimitedUseCard && card.DeckVersion is not null && card.Owner == Owner.Player;
}
