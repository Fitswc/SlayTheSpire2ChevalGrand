using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ChevalGrandSlay.Mechanics;

internal static class CGSRandomRestRecovery
{
    public static async Task RecoverFully(CardModel source, int count)
    {
        // 候选来自持有者的休整牌堆，排除触发效果的牌和无法恢复体力的牌。
        var candidates = Entry.CGSFatePile.GetPile(source.Owner).Cards
            .Where(card => card != source && CGSStamina.CanRecoverStamina(card))
            .ToList();
        var selected = new List<CardModel>();

        // 先用战斗随机数从候选中不放回地选牌，最多选 count 张。整批选完后再移牌，避免移牌触发的效果干扰选择。
        while (selected.Count < count && candidates.Count > 0)
        {
            var card = source.Owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
            if (card is null)
                break;
            candidates.Remove(card);
            selected.Add(card);
        }

        // 前一张牌移入手牌时可能触发其他效果。恢复前再次确认目标仍在休整牌堆。
        foreach (var card in selected)
        {
            if (card.Pile?.Type != Entry.CGSFatePile)
                continue;

            CGSStamina.RestoreFull(card);
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }
}
