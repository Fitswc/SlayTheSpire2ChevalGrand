using System.Reflection;
using ChevalGrandSlay.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace ChevalGrandSlay;

[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.MutableClone))]
internal static class CGSStaminaCopyPatch
{
    [HarmonyPrepare]
    private static void Prepare(MethodBase? original)
    {
        // original 为空时是整个补丁的回调，避免逐个目标重复输出。
        if (original == null)
            Entry.Logger.Info($"[Patch] {nameof(CGSStaminaCopyPatch)} Star Patching");
    }

    [HarmonyCleanup]
    private static void Cleanup(MethodBase? original, Exception? exception)
    {
        if (original != null)
            return;

        // 游戏日志接口同时输出到控制台和日志；
        if (exception == null)
            Entry.Logger.Info($"[Patch] {nameof(CGSStaminaCopyPatch)} Patch Successful");
        else
            Entry.Logger.Error($"[Patch] {nameof(CGSStaminaCopyPatch)} Patch Failed: {exception}");
    }

    [HarmonyPostfix]
    public static void Postfix(AbstractModel __instance, ref AbstractModel __result)
    {
        if (__instance is not CardModel original || __result is not CardModel copy)
            return;

        // 只处理本模组的卡牌。
        if (original.GetType().Assembly != typeof(CGSStaminaCopyPatch).Assembly)
            return;

        // 原卡与副本都必须具有完整的耐力变量。
        if (!original.DynamicVars.TryGetValue("MaxUses", out var originalMaxUses) ||
            !original.DynamicVars.TryGetValue("Uses", out var originalUses) ||
            !copy.DynamicVars.TryGetValue("Uses", out var copiedUses) ||
            !copy.DynamicVars.TryGetValue("MaxUses", out var copiedMaxUses))
            return;

        // 耐力耗尽时，复制出来的是空卡牌，归属仍与原卡一致。
        if (originalUses.BaseValue <= 0m)
        {
            var emptyShell = ModelDb.Card<CGSEmptyShell>().ToMutable();
            emptyShell.Owner = original.Owner;
            // 副本仍由调用方注册到对应的卡牌作用域。
            __result = emptyShell;
            return;
        }

        // 完整保留当前耐力与上限，包括升级和战斗中的修改。
        copiedMaxUses.BaseValue = originalMaxUses.BaseValue;
        copiedUses.BaseValue = originalUses.BaseValue;
    }
}
