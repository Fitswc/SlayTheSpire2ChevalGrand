using ChevalGrandSlay.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace ChevalGrandSlay;

[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.MutableClone))]
internal static class CGSStaminaCopyPatch
{
    [HarmonyPostfix]
    public static void Postfix(AbstractModel __instance, ref AbstractModel __result)
    {
        if (__instance is not CardModel original || __result is not CardModel copy ||
            original.GetType().Assembly != typeof(CGSStaminaCopyPatch).Assembly ||
            !original.DynamicVars.TryGetValue("MaxUses", out var originalMaxUses) ||
            !original.DynamicVars.TryGetValue("Uses", out var originalUses) ||
            !copy.DynamicVars.TryGetValue("Uses", out var copiedUses) ||
            !copy.DynamicVars.TryGetValue("MaxUses", out var copiedMaxUses))
            return;

        if (originalUses.BaseValue <= 0m)
        {
            var emptyShell = ModelDb.Card<CGSEmptyShell>().ToMutable();
            emptyShell.Owner = original.Owner;
            // The caller retains responsibility for registering the clone in its card scope.
            __result = emptyShell;
            return;
        }

        // Assign both values exactly, including upgrades and changes made during combat.
        copiedMaxUses.BaseValue = originalMaxUses.BaseValue;
        copiedUses.BaseValue = originalUses.BaseValue;
    }
}
