using System.Runtime.CompilerServices;
using ChevalGrandSlay.Enchantments;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ChevalGrandSlay.Mechanics;

internal static class CGSConsumeUse
{
    private sealed class PlayDecision(bool preserved)
    {
        public bool Preserved { get; } = preserved;
    }

    private static readonly ConditionalWeakTable<CardModel, PlayDecision> Decisions = new();

    public static PileType GetResultPileTypeForCardPlay(CardModel card, PileType normalPile)
    {
        // The game chooses the result pile before calling OnPlay, where Uses is decremented.
        bool preserved = TryPreserve(card);
        Decisions.Remove(card);
        Decisions.Add(card, new PlayDecision(preserved));

        return card.DynamicVars["Uses"].IntValue <= (preserved ? 0 : 1)
            ? Entry.CGSFatePile
            : normalPile;
    }

    public static void Consume(CardModel card)
    {
        if (Decisions.TryGetValue(card, out var decision))
        {
            Decisions.Remove(card);
            if (decision.Preserved)
                return;
        }
        else if (TryPreserve(card))
            return;

        var uses = card.DynamicVars["Uses"];
        uses.BaseValue = Math.Max(0m, uses.BaseValue - 1m);
    }

    private static bool TryPreserve(CardModel card) =>
        // 无尽体力保护所有牌，预留一步在本场战斗中保护指定的牌。
        card.Owner.Creature.HasPower<CGSEndlessStaminaPower>() ||
        (card.Enchantment is CGSReserveStepEnchantment enchantment && enchantment.PreservesStamina(card));
}
