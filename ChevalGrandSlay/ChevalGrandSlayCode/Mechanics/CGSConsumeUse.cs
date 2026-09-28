using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Models;

namespace ChevalGrandSlay.Mechanics;

internal static class CGSConsumeUse
{
    public static void Consume(CardModel card)
    {
        if (card.Owner.Creature.Powers.OfType<CGSReserveAStepPower>().Any(power => power.TryPreserve(card)))
            return;

        var uses = card.DynamicVars["Uses"];
        uses.BaseValue = Math.Max(0m, uses.BaseValue - 1m);
    }
}
