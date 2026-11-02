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
}
