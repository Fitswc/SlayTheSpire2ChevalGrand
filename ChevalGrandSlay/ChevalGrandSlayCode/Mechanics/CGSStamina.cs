using System.Runtime.CompilerServices;
using ChevalGrandSlay.Compatibility;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib;
using STS2RitsuLib.Content;
using STS2RitsuLib.Models.Capabilities;

namespace ChevalGrandSlay.Mechanics;

internal static class CGSStamina
{
    // 卡牌动态变量记录当前体力和体力上限，供战斗结算和卡面显示读取。
    internal const string Uses = "CGSUses";
    internal const string MaxUses = "CGSMaxUses";

    // 标记尚未执行开局移牌的战斗，确保只在玩家第一回合处理一次。
    private static readonly ConditionalWeakTable<ICombatState, object> PendingFirstTurns = new();

    // 牌组原卡的存档记录体力缺额、休整牌堆顺序和锁定状态。
    private sealed record SpentStamina
    {
        public decimal Amount { get; init; }
        public int? RestOrder { get; init; }
        public bool Locked { get; init; }
    }

    private static ModelSavedData<CardModel, SpentStamina> _spentStamina = null!;

    internal static void Register()
    {
        // 给模组自带的体力牌和兼容层生成牌注册体力消耗及锁定叠层。
        ModContentRegistry.For(Entry.ModId).ConfigureDefaultModelCapabilities<CardModel>(
            "stamina-capabilities", static (card, capabilities) =>
            {
                if (card is CGSStaminaCard or ICGSGeneratedCard)
                {
                    capabilities.Add<CGSConsumeUse>();
                    capabilities.Add<CGSStaminaLockOverlay>();
                }
            });

        // 由框架在克隆时复制存档数据，仅写入非默认记录。
        _spentStamina = RitsuLibFramework.GetModelSavedDataStore(Entry.ModId)
            .Register<CardModel, SpentStamina>("spent-stamina", options: new ModelSavedDataOptions
            {
                ClonePolicy = ModelSavedDataClonePolicy.Copy,
                WritePolicy = ModelSavedDataWritePolicy.WhenNonDefault
            });

        // 开战读取体力，首回合抽牌前处理休整牌，战斗结束时保存牌堆顺序。
        RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(LoadCombatStamina, replayCurrentState: false);
        RitsuLibFramework.SubscribeLifecycle<SideTurnStartingEvent>(MoveStartingCardsToRest, replayCurrentState: false);
        RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(SaveRestOrder, replayCurrentState: false);
    }

    // 体力牌需要同时定义当前值和上限。尝试恢复时还要检查原卡的锁定状态。
    public static bool HasStamina(CardModel card) =>
        card.DynamicVars.ContainsKey(Uses) && card.DynamicVars.ContainsKey(MaxUses);

    internal static bool IsLocked(CardModel card) =>
        _spentStamina.TryGet(card.DeckVersion ?? card, out var spent) && spent.Locked;

    internal static bool CanRecoverStamina(CardModel card) => HasStamina(card) && !IsLocked(card);

    // 休息处只更新牌组原卡的存档。下次开战时按该记录处理对应的战斗副本。
    internal static void LockDeckCard(CardModel card) =>
        _spentStamina.Set(card, new SpentStamina
        {
            RestOrder = GetRestOrder(card),
            Locked = true
        });

    // 出牌门槛用整数体力判断，耗尽和回满则按动态变量的实际数值判断。
    public static int GetRemaining(CardModel card) => card.DynamicVars[Uses].IntValue;

    public static bool HasAtLeast(CardModel card, int amount) =>
        HasStamina(card) && GetRemaining(card) >= amount;

    public static bool IsDepleted(CardModel card) => card.DynamicVars[Uses].BaseValue <= 0m;

    public static bool IsFull(CardModel card) => card.DynamicVars[Uses].BaseValue >= card.DynamicVars[MaxUses].BaseValue;

    // 消耗体力和移入休整牌堆是两个步骤。调用方可选择仅扣除、耗尽时移牌或直接清零移牌。
    public static void Spend(CardModel card, decimal amount) =>
        SetRemaining(card, card.DynamicVars[Uses].BaseValue - amount);

    public static async Task SpendAndRestIfDepleted(CardModel card, decimal amount)
    {
        Spend(card, amount);
        if (GetRemaining(card) == 0)
            await CardPileCmd.Add(card, Entry.CGSFatePile);
    }

    public static Task DepleteAndRest(CardModel card)
    {
        SetRemaining(card, 0m);
        return CardPileCmd.Add(card, Entry.CGSFatePile);
    }

    // 常规恢复受上限约束。特定效果可显式允许溢出，或直接恢复至上限。
    public static void Restore(CardModel card, decimal amount) =>
        SetRemaining(card, card.DynamicVars[Uses].BaseValue + amount);

    public static void RestoreBeyondMaximum(CardModel card, decimal amount) =>
        SetRemaining(card, card.DynamicVars[Uses].BaseValue + amount, allowExceedMaximum: true);

    public static void RestoreFull(CardModel card) =>
        SetRemaining(card, card.DynamicVars[MaxUses].BaseValue);

    // 先固定候选名单，再逐张恢复仍在休整牌堆的有效目标。回满的牌才返回抽牌堆。
    internal static async Task RecoverRestingCardsToDraw(IEnumerable<CardModel> cards, decimal amount)
    {
        foreach (var card in cards.ToArray())
        {
            if (card.Pile?.Type != Entry.CGSFatePile || !CanRecoverStamina(card))
                continue;

            Restore(card, amount);
            if (IsFull(card))
                await CardPileCmd.Add(card, PileType.Draw);
        }
    }

    // 未锁定的体力牌升级时补满体力。若卡牌已在牌堆中，还要清除对应原卡的体力缺额。
    internal static void RestoreAfterUpgrade(CardModel card)
    {
        if (!CanRecoverStamina(card))
            return;

        var remaining = card.DynamicVars[Uses];
        var maximum = card.DynamicVars[MaxUses].BaseValue;
        if (remaining.BaseValue != maximum)
            remaining.UpgradeValueBy(maximum - remaining.BaseValue);

        // 预览和反序列化时的副本尚未进入牌堆，跳过对牌组存档的写入。
        if (card.Pile is not { } pile)
            return;

        if (pile.Type == PileType.Deck)
            RestoreDeckCard(card);
        else if (card.DeckVersion is { } deckCard)
            RestoreDeckCard(deckCard);
    }

    // 牌组界面按基础值减去已保存的缺额显示体力，已锁定时直接显示 0。
    internal static decimal GetDeckStamina(CardModel card)
    {
        decimal remaining = card.DynamicVars[Uses].BaseValue;
        if (!_spentStamina.TryGet(card, out var spent))
            return remaining;

        return spent.Locked ? 0m : ClampRemaining(card, remaining - spent.Amount);
    }

    // 牌组恢复通过删除存档记录清除缺额。锁定记录必须保留。
    internal static bool CanRestoreDeckCard(CardModel card) =>
        HasStamina(card) && _spentStamina.TryGet(card, out var spent) && !spent.Locked && spent.Amount > 0m;

    internal static bool RestoreDeckCard(CardModel card) =>
        !IsLocked(card) && _spentStamina.Remove(card);

    // 增加上限、设定上限并补满，以及复制体力都经同一个写入入口约束数值。
    public static void IncreaseMaximumAndRemaining(CardModel card, decimal amount) =>
        SetStamina(card, card.DynamicVars[Uses].BaseValue + amount,
            card.DynamicVars[MaxUses].BaseValue + amount);

    public static void SetMaximumAndFill(CardModel card, decimal maximum) =>
        SetStamina(card, maximum, maximum);

    public static void Copy(CardModel source, CardModel destination) =>
        SetStamina(destination, source.DynamicVars[Uses].BaseValue, source.DynamicVars[MaxUses].BaseValue);

    private static void SetStamina(CardModel card, decimal remaining, decimal maximum)
    {
        card.DynamicVars[MaxUses].BaseValue = Math.Max(0m, maximum);
        card.DynamicVars[Uses].BaseValue = IsLocked(card) ? 0m : ClampRemaining(card, remaining);
    }

    // 战斗中更新剩余体力后，将缺额同步到牌组原卡。锁定牌跳过更新。
    private static void SetRemaining(CardModel card, decimal remaining, bool allowExceedMaximum = false)
    {
        if (IsLocked(card))
            return;

        remaining = allowExceedMaximum
            ? Math.Max(0m, remaining)
            : ClampRemaining(card, remaining);
        card.DynamicVars[Uses].BaseValue = remaining;

        if (card.DeckVersion is not { } deckCard || !HasStamina(deckCard))
            return;

        // 保存距离上限的缺额，便于换用新的上限后重新计算。缺额归零时清除记录。
        decimal spent = ClampRemaining(deckCard, card.DynamicVars[MaxUses].BaseValue - remaining);
        if (spent == 0m)
            _spentStamina.Remove(deckCard);
        else
            _spentStamina.Set(deckCard, new SpentStamina { Amount = spent, RestOrder = GetRestOrder(deckCard) });
    }

    private static decimal ClampRemaining(CardModel card, decimal remaining) =>
        Math.Clamp(remaining, 0m, card.DynamicVars[MaxUses].BaseValue);

    // 初始化战斗副本时应用原卡存档。普通体力牌扣除已存缺额，锁定牌直接归零。
    private static void LoadCombatStamina(CombatStartingEvent starting)
    {
        if (starting.CombatState is not { } state)
            return;

        PendingFirstTurns.GetValue(state, static _ => new object());

        foreach (var player in state.Players)
        {
            if (player.PlayerCombatState is not { } combat)
                continue;

            foreach (var card in combat.AllCards)
            {
                if (card.DeckVersion is { } deckCard && HasStamina(card) &&
                    _spentStamina.TryGet(deckCard, out var spent))
                    card.DynamicVars[Uses].BaseValue = spent.Locked
                        ? 0m
                        : ClampRemaining(card, card.DynamicVars[Uses].BaseValue - spent.Amount);
            }
        }
    }

    private static int? GetRestOrder(CardModel deckCard) =>
        _spentStamina.TryGet(deckCard, out var spent) ? spent.RestOrder : null;

    private static void MoveStartingCardsToRest(SideTurnStartingEvent starting)
    {
        if (starting.Side != CombatSide.Player || !PendingFirstTurns.Remove(starting.CombatState))
            return;

        // 开战前的钩子可能补充临时体力，等它们结算完，再于抽牌前判定哪些牌需要休整。
        foreach (var player in starting.CombatState.Players)
        {
            if (player.PlayerCombatState?.DrawPile is not { } drawPile)
                continue;

            // 锁定、耗尽或上场仍在休整且尚未回满的牌按存档顺序入堆。无顺序记录的牌排在末尾。
            var restingCards = drawPile.Cards
                .Where(card => card.DeckVersion is not null && HasStamina(card))
                .Select(card => (Card: card, Order: GetRestOrder(card.DeckVersion!)))
                .Where(entry => IsLocked(entry.Card) || IsDepleted(entry.Card) ||
                    (entry.Order.HasValue && !IsFull(entry.Card)))
                .OrderBy(entry => entry.Order ?? int.MaxValue)
                .Select(entry => entry.Card)
                .ToArray();
            if (restingCards.Length == 0)
                continue;

            var restPile = Entry.CGSFatePile.GetPile(player);
            foreach (var card in restingCards)
            {
                drawPile.RemoveInternal(card);
                restPile.AddInternal(card);
            }
        }
    }

    // 战斗结束时按原卡记录休整牌堆的相对顺序，供下一场战斗恢复。
    private static void SaveRestOrder(CombatEndedEvent ending)
    {
        if (ending.CombatState is not { } state)
            return;

        foreach (var player in state.Players)
        {
            if (player.PlayerCombatState is null)
                continue;

            var orderByDeckCard = new Dictionary<CardModel, int>(ReferenceEqualityComparer.Instance);
            foreach (var card in Entry.CGSFatePile.GetPile(player).Cards)
            {
                if (card.DeckVersion is { } deckCard && HasStamina(card) && !IsFull(card))
                    orderByDeckCard.TryAdd(deckCard, orderByDeckCard.Count);
            }

            // 只更新已有体力存档的牌。已离开休整牌堆的牌清除顺序，其余字段照旧保留。
            foreach (var deckCard in player.Deck.Cards)
            {
                if (!_spentStamina.TryGet(deckCard, out var spent))
                    continue;

                int? restOrder = orderByDeckCard.TryGetValue(deckCard, out int index) ? index : null;
                if (spent.RestOrder != restOrder)
                    _spentStamina.Set(deckCard, spent with { RestOrder = restOrder });
            }
        }
    }
}
