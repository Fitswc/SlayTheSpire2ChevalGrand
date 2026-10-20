using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ChevalGrandSlay.Mechanics;

internal static class CGSRandomRestRecovery
{
    public static async Task Recover(CardModel source, int count, decimal restore, CardType? type = null)
    {
        var candidates = Entry.CGSFatePile.GetPile(source.Owner).Cards
            .Where(card => card != source &&
                card.DynamicVars.ContainsKey("Uses") &&
                card.DynamicVars.ContainsKey("MaxUses") &&
                (type is null || card.Type == type))
            .ToList();
        var selected = new List<CardModel>();

        // Choose without replacement before moving cards, so triggered effects cannot alter the draw.
        while (selected.Count < count && candidates.Count > 0)
        {
            var card = source.Owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
            if (card is null)
                break;
            candidates.Remove(card);
            selected.Add(card);
        }

        foreach (var card in selected)
        {
            if (card.Pile?.Type != Entry.CGSFatePile)
                continue;

            var uses = card.DynamicVars["Uses"];
            uses.BaseValue = Math.Min(card.DynamicVars["MaxUses"].BaseValue, uses.BaseValue + restore);
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }
}
