using System.Runtime.CompilerServices;
using ChevalGrandSlay.Enchantments;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace ChevalGrandSlay.Mechanics;

[RegisterModelCapability]
public sealed class CGSConsumeUse : CardCapability, ICardPlayResultContributor
{
    private static readonly ConditionalWeakTable<CardModel, PlayDecision> Decisions = new();

    protected override void OnOwnerCardUpgraded(CardModel card) => CGSStamina.RestoreAfterUpgrade(card);

    public PileType? GetResultPileTypeForCardPlay(CardModel card)
    {
        if (!CGSStamina.HasStamina(card))
            return null;

        // The game chooses the result pile before calling OnPlay, where Stamina is spent.
        bool preserved = TryPreserve(card);
        Decisions.Remove(card);
        Decisions.Add(card, new PlayDecision(preserved));

        return CGSStamina.GetRemaining(card) <= (preserved ? 0 : 1) ? Entry.CGSFatePile : null;
    }

    internal static void Consume(CardModel card)
    {
        if (Decisions.TryGetValue(card, out var decision))
        {
            Decisions.Remove(card);
            if (decision.Preserved)
                return;
        }
        else if (TryPreserve(card))
            return;

        CGSStamina.Spend(card, 1m);
    }

    private static bool TryPreserve(CardModel card) =>
        // 无尽体力保护所有牌，再钓一条在本场战斗中保护指定的牌。
        card.Owner.Creature.HasPower<CGSEndlessStaminaPower>() ||
        (card.Enchantment is CGSReserveStepEnchantment enchantment && enchantment.PreservesStamina(card));

    private sealed class PlayDecision(bool preserved)
    {
        public bool Preserved { get; } = preserved;
    }
}
