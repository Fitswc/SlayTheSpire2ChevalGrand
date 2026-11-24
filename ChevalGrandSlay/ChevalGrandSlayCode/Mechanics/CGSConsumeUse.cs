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
    // 按卡牌实例暂存本次出牌是否免耗。选定去向和实际扣除体力时共用这次判定。
    private static readonly ConditionalWeakTable<CardModel, PlayDecision> Decisions = new();

    // 升级后按体力规则补满。锁定状态由体力机制检查。
    protected override void OnOwnerCardUpgraded(CardModel card) => CGSStamina.RestoreAfterUpgrade(card);

    public PileType? GetResultPileTypeForCardPlay(CardModel card)
    {
        if (!CGSStamina.HasStamina(card))
            return null;

        // 游戏先决定出牌后的去向，再调用 OnPlay 扣除体力。预判体力会归零时，将牌送入休整牌堆。
        bool preserved = TryPreserve(card);
        Decisions.Remove(card);
        Decisions.Add(card, new PlayDecision(preserved));

        return CGSStamina.GetRemaining(card) <= (preserved ? 0 : 1) ? Entry.CGSFatePile : null;
    }

    // 实际结算时取出并清除暂存的判定。未经过牌堆判定的路径在这里检查免耗效果。
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

    // 无尽体力检查持有者的能力，再钓一条通过该牌上的附魔判定是否免耗。
    private static bool TryPreserve(CardModel card) =>
        card.Owner.Creature.HasPower<CGSEndlessStaminaPower>() ||
        (card.Enchantment is CGSReserveStepEnchantment enchantment && enchantment.PreservesStamina(card));

    private sealed class PlayDecision(bool preserved)
    {
        public bool Preserved { get; } = preserved;
    }
}
