using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using STS2RitsuLib.Patching.Models;

namespace ChevalGrandSlay;

// ShouldTakeExtraTurn 的模型钩子采用“任一同意即生效”，需在入口否决其他来源。
internal sealed class CGSOneBeatAheadPatch : IPatchMethod
{
    public static string PatchId => "one_beat_ahead";

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(Hook), nameof(Hook.ShouldTakeExtraTurn))];

    private static bool Prefix(Player player, ref bool __result)
    {
        if (player.Creature.GetPower<CGSOneBeatAheadPower>()?.IsExtraTurnActive != true)
            return true;

        __result = false;
        return false;
    }
}
