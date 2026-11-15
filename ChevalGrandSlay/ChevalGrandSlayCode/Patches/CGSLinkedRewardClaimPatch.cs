using System.Reflection;
using System.Reflection.Emit;
using ChevalGrandSlayCode.Rewards;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rewards;

namespace ChevalGrandSlay;

[HarmonyPatch(typeof(NLinkedRewardSet), "Reload")]
internal static class CGSLinkedRewardClaimPatch
{
    [HarmonyPrepare]
    private static void Prepare(MethodBase? original)
    {
        // original 为空时是整个补丁的回调，避免逐个目标重复输出。
        if (original == null)
            Entry.Logger.Info($"[Patch] {nameof(CGSLinkedRewardClaimPatch)} Star Patching");
    }

    [HarmonyCleanup]
    private static void Cleanup(MethodBase? original, Exception? exception)
    {
        if (original != null)
            return;

        // 游戏日志接口同时输出到控制台和日志；
        if (exception == null)
            Entry.Logger.Info($"[Patch] {nameof(CGSLinkedRewardClaimPatch)} Patch Successful");
        else
            Entry.Logger.Error($"[Patch] {nameof(CGSLinkedRewardClaimPatch)} Patch Failed: {exception}");
    }
    
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions).MatchStartForward(
            new CodeMatch(OpCodes.Ldftn, AccessTools.Method(typeof(NLinkedRewardSet), "GetReward")),
            new CodeMatch(OpCodes.Newobj),
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Callable), nameof(Callable.From), [typeof(Action)])));

        if (matcher.IsValid)
            matcher.Advance(2).SetOperandAndAdvance(AccessTools.Method(
                typeof(CGSLinkedRewardClaimPatch), nameof(CreateClaimCallback)));

        return matcher.InstructionEnumeration();
    }

    private static Callable CreateClaimCallback(Action original)
    {
        if (original.Target is not NLinkedRewardSet node ||
            !node.LinkedRewardSet.Rewards.Any(reward => reward is CGSRewardAfterCombat or CGSRestoreStaminaReward))
            return Callable.From(original);

        // The child sends its button. The screen expects the linked node and removes it when notified.
        return Callable.From<NRewardButton>(_ =>
        {
            if (node.IsQueuedForDeletion())
                return;

            node.LinkedRewardSet.OnSkipped();
            node.EmitSignal(NLinkedRewardSet.SignalName.RewardClaimed, node);
        });
    }
}
