using System.Runtime.CompilerServices;
using ChevalGrandSlay.Compatibility;
using ChevalGrandSlay.Relics;
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
    internal const string Uses = "CGSUses";
    internal const string MaxUses = "CGSMaxUses";

    private static readonly ConditionalWeakTable<ICombatState, object> PendingFirstTurns = new();
    private static readonly ConditionalWeakTable<CardModel, object> EmptyCopies = new();

    private sealed record SpentStamina
    {
        public decimal Amount { get; init; }
        public int? RestOrder { get; init; }
    }

    private static ModelSavedData<CardModel, SpentStamina> _spentStamina = null!;

    internal static void Register()
    {
        ModContentRegistry.For(Entry.ModId).ConfigureDefaultModelCapabilities<CardModel>(
            "stamina-result-pile", static (card, capabilities) =>
            {
                if (card is CGSStaminaCard or ICGSGeneratedCard)
                    capabilities.Add<CGSConsumeUse>();
            });

        _spentStamina = RitsuLibFramework.GetModelSavedDataStore(Entry.ModId)
            .Register<CardModel, SpentStamina>("spent-stamina", options: new ModelSavedDataOptions
            {
                ClonePolicy = ModelSavedDataClonePolicy.Copy,
                WritePolicy = ModelSavedDataWritePolicy.WhenNonDefault
            });

        RitsuLibFramework.GetModelCloneRegistry(Entry.ModId)
            .Register<CardModel>("copy-stamina", OnCardCloned);

        RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(LoadCombatStamina, replayCurrentState: false);
        RitsuLibFramework.SubscribeLifecycle<SideTurnStartingEvent>(MoveStartingCardsToRest, replayCurrentState: false);
        RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(SaveRestOrder, replayCurrentState: false);
    }

    internal static bool IsEmptyCopy(CardModel card) => EmptyCopies.TryGetValue(card, out _);

    private static void OnCardCloned(CardModel original, CardModel copy)
    {
        if (original is not (CGSStaminaCard or ICGSGeneratedCard) || !HasStamina(original))
            return;

        if (IsEmptyCopy(original) || IsDepleted(original))
        {
            EmptyCopies.GetOrCreateValue(copy);
            foreach (var keyword in copy.Keywords.ToArray())
                copy.RemoveKeyword(keyword);
            copy.AddKeyword(CardKeyword.Exhaust);
            if (!copy.EnergyCost.CostsX)
                copy.EnergyCost.SetThisCombat(0);
            return;
        }

        Copy(original, copy);
        CGSStartRelicPlus.InheritStaminaBonus(original, copy);
    }

    public static bool HasStamina(CardModel card) =>
        card.DynamicVars.ContainsKey(Uses) && card.DynamicVars.ContainsKey(MaxUses);

    public static int GetRemaining(CardModel card) => card.DynamicVars[Uses].IntValue;

    public static bool HasAtLeast(CardModel card, int amount) =>
        HasStamina(card) && GetRemaining(card) >= amount;

    public static bool IsDepleted(CardModel card) => card.DynamicVars[Uses].BaseValue <= 0m;

    public static bool IsFull(CardModel card) => card.DynamicVars[Uses].BaseValue >= card.DynamicVars[MaxUses].BaseValue;

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

    public static void Restore(CardModel card, decimal amount) =>
        SetRemaining(card, card.DynamicVars[Uses].BaseValue + amount);

    public static void RestoreBeyondMaximum(CardModel card, decimal amount) =>
        SetRemaining(card, card.DynamicVars[Uses].BaseValue + amount, allowExceedMaximum: true);

    public static void RestoreFull(CardModel card) =>
        SetRemaining(card, card.DynamicVars[MaxUses].BaseValue);

    internal static async Task RecoverRestingCardsToDraw(IEnumerable<CardModel> cards, decimal amount)
    {
        foreach (var card in cards.ToArray())
        {
            if (card.Pile?.Type != Entry.CGSFatePile || !HasStamina(card))
                continue;

            Restore(card, amount);
            if (IsFull(card))
                await CardPileCmd.Add(card, PileType.Draw);
        }
    }

    internal static void RestoreAfterUpgrade(CardModel card)
    {
        if (!HasStamina(card))
            return;

        var remaining = card.DynamicVars[Uses];
        var maximum = card.DynamicVars[MaxUses].BaseValue;
        if (remaining.BaseValue != maximum)
            remaining.UpgradeValueBy(maximum - remaining.BaseValue);

        // Preview and deserialization clones are outside their owner's piles.
        if (card.Pile is not { } pile)
            return;

        if (pile.Type == PileType.Deck)
            RestoreDeckCard(card);
        else if (card.DeckVersion is { } deckCard)
            RestoreDeckCard(deckCard);
    }

    internal static decimal GetDeckStamina(CardModel card) =>
        _spentStamina.TryGet(card, out var spent)
            ? ClampRemaining(card, card.DynamicVars[Uses].BaseValue - spent.Amount)
            : card.DynamicVars[Uses].BaseValue;

    internal static bool CanRestoreDeckCard(CardModel card) =>
        HasStamina(card) && _spentStamina.TryGet(card, out var spent) && spent.Amount > 0m;

    internal static bool RestoreDeckCard(CardModel card) =>
        _spentStamina.Remove(card);

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
        card.DynamicVars[Uses].BaseValue = ClampRemaining(card, remaining);
    }

    private static void SetRemaining(CardModel card, decimal remaining, bool allowExceedMaximum = false)
    {
        remaining = allowExceedMaximum
            ? Math.Max(0m, remaining)
            : ClampRemaining(card, remaining);
        card.DynamicVars[Uses].BaseValue = remaining;

        if (card.DeckVersion is not { } deckCard || !HasStamina(deckCard))
            return;

        // Keep the missing amount so upgrades and temporary bonuses retain their existing behavior.
        decimal spent = ClampRemaining(deckCard, card.DynamicVars[MaxUses].BaseValue - remaining);
        if (spent == 0m)
            _spentStamina.Remove(deckCard);
        else
            _spentStamina.Set(deckCard, new SpentStamina { Amount = spent, RestOrder = GetRestOrder(deckCard) });
    }

    private static decimal ClampRemaining(CardModel card, decimal remaining) =>
        Math.Clamp(remaining, 0m, card.DynamicVars[MaxUses].BaseValue);

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
                    card.DynamicVars[Uses].BaseValue = ClampRemaining(card, card.DynamicVars[Uses].BaseValue - spent.Amount);
            }
        }
    }

    private static int? GetRestOrder(CardModel deckCard) =>
        _spentStamina.TryGet(deckCard, out var spent) ? spent.RestOrder : null;

    private static void MoveStartingCardsToRest(SideTurnStartingEvent starting)
    {
        if (starting.Side != CombatSide.Player || !PendingFirstTurns.Remove(starting.CombatState))
            return;

        // BeforeCombatStart hooks can grant temporary Stamina. Check after those hooks and before drawing.
        foreach (var player in starting.CombatState.Players)
        {
            if (player.PlayerCombatState?.DrawPile is not { } drawPile)
                continue;

            var restingCards = drawPile.Cards
                .Where(card => card.DeckVersion is not null && HasStamina(card))
                .Select(card => (Card: card, Order: GetRestOrder(card.DeckVersion!)))
                .Where(entry => IsDepleted(entry.Card) || (entry.Order.HasValue && !IsFull(entry.Card)))
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
