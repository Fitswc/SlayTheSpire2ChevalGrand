using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using STS2RitsuLib.Patching.Models;

namespace ChevalGrandSlay;

// 额外回合钩子只要有一个来源同意就会生效，因此需在游戏汇总判断前拦截。
internal sealed class CGSOneBeatAheadPatch : IPatchMethod
{
    public static string PatchId => "CGSOneBeatAhead";

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(Hook), nameof(Hook.ShouldTakeExtraTurn))];

    // 该能力授予的额外回合进行中时，直接返回 false，阻止其他钩子再追加回合。
    private static bool Prefix(Player player, ref bool __result)
    {
        if (player.Creature.GetPower<CGSOneBeatAheadPower>()?.IsExtraTurnActive != true)
            return true;

        __result = false;
        return false;
    }
}
