using System.Reflection;
using ChevalGrandSlay.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;

namespace ChevalGrandSlay;

// ShouldTakeExtraTurn 的模型钩子采用“任一同意即生效”，需在入口否决其他来源。
[HarmonyPatch(typeof(Hook), nameof(Hook.ShouldTakeExtraTurn))]
internal static class CGSOneBeatAheadPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Player player, ref bool __result)
    {
        if (player.Creature.GetPower<CGSOneBeatAheadPower>()?.IsExtraTurnActive != true)
            return true;

        __result = false;
        return false;
    }
    
    [HarmonyPrepare]
    private static void Prepare(MethodBase? original)
    {
        // original 为空时是整个补丁的回调，避免逐个目标重复输出。
        if (original == null)
            Entry.Logger.Info($"[Patch] {nameof(CGSCharacterSelectAudio)} Start Patching");
    }
    
    [HarmonyCleanup]
    private static void Cleanup(MethodBase? original, Exception? exception)
    {
        if (original != null)
            return;

        // 游戏日志接口同时输出到控制台和日志；
        if (exception == null)
            Entry.Logger.Info($"[Patch] {nameof(CGSOneBeatAheadPatch)} Patch Successful");
        else
            Entry.Logger.Error($"[Patch] {nameof(CGSOneBeatAheadPatch)} Patch Failed: {exception}");
    }
}
