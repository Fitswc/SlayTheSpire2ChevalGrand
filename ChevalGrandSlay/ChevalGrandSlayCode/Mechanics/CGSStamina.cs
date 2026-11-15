using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib;
using STS2RitsuLib.Models.Capabilities;

namespace ChevalGrandSlay.Mechanics;

internal static class CGSStamina
{
    internal const string Uses = "Uses";
    internal const string MaxUses = "MaxUses";

    private sealed record SpentStamina
    {
        public decimal Amount { get; init; }
    }

    private static ModelSavedData<CardModel, SpentStamina> _spentStamina = null!;

    internal static void Register()
    {
        _spentStamina = RitsuLibFramework.GetModelSavedDataStore(Entry.ModId)
            .Register<CardModel, SpentStamina>("spent-stamina", options: new ModelSavedDataOptions
            {
                ClonePolicy = ModelSavedDataClonePolicy.Copy,
                WritePolicy = ModelSavedDataWritePolicy.WhenNonDefault
            });

        RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(LoadCombatStamina, replayCurrentState: false);
        CombatManager.Instance.CombatBegan += MoveDepletedCardsToRest;
    }

    public static bool HasStamina(CardModel card) =>
        card.DynamicVars.ContainsKey(Uses) && card.DynamicVars.ContainsKey(MaxUses);

    public static int GetRemaining(CardModel card) => card.DynamicVars[Uses].IntValue;

    public static bool HasAtLeast(CardModel card, int amount) =>
        HasStamina(card) && GetRemaining(card) >= amount;

    public static bool IsDepleted(CardModel card) => card.DynamicVars[Uses].BaseValue <= 0m;

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

    public static void RestoreFull(CardModel card) =>
        SetRemaining(card, card.DynamicVars[MaxUses].BaseValue);

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

    private static void SetRemaining(CardModel card, decimal remaining)
    {
        remaining = ClampRemaining(card, remaining);
        card.DynamicVars[Uses].BaseValue = remaining;

        if (card.DeckVersion is not { } deckCard || !HasStamina(deckCard))
            return;

        // Keep the missing amount so upgrades and temporary bonuses retain their existing behavior.
        decimal spent = ClampRemaining(deckCard, card.DynamicVars[MaxUses].BaseValue - remaining);
        if (spent == 0m)
            _spentStamina.Remove(deckCard);
        else
            _spentStamina.Set(deckCard, new SpentStamina { Amount = spent });
    }

    private static decimal ClampRemaining(CardModel card, decimal remaining) =>
        Math.Clamp(remaining, 0m, card.DynamicVars[MaxUses].BaseValue);

    private static void LoadCombatStamina(CombatStartingEvent starting)
    {
        if (starting.CombatState is not { } state)
            return;

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

    private static void MoveDepletedCardsToRest(CombatState combat)
    {
        // BeforeCombatStart hooks can grant temporary Stamina. Check after those hooks and before drawing.
        foreach (var player in combat.Players)
        {
            if (player.PlayerCombatState?.DrawPile is not { } drawPile)
                continue;

            var depleted = drawPile.Cards
                .Where(card => card.DeckVersion is not null && HasStamina(card) && IsDepleted(card))
                .ToArray();
            if (depleted.Length == 0)
                continue;

            var restPile = Entry.CGSFatePile.GetPile(player);
            foreach (var card in depleted)
            {
                drawPile.RemoveInternal(card);
                restPile.AddInternal(card);
            }
        }
    }
}
