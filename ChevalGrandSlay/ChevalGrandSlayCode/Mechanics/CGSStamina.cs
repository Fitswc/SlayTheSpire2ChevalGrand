using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;

namespace ChevalGrandSlay.Mechanics;

internal static class CGSStamina
{
    internal const string Uses = "Uses";
    internal const string MaxUses = "MaxUses";

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
        SetRemaining(card, remaining);
    }

    private static void SetRemaining(CardModel card, decimal remaining) =>
        card.DynamicVars[Uses].BaseValue = Math.Clamp(remaining, 0m, card.DynamicVars[MaxUses].BaseValue);
}
