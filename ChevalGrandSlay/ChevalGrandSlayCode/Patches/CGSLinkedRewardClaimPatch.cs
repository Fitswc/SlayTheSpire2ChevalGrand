using System.Reflection;
using System.Reflection.Emit;
using ChevalGrandSlayCode.Rewards;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using STS2RitsuLib.Patching.Models;

namespace ChevalGrandSlay;

internal sealed class CGSLinkedRewardClaimPatch : IPatchMethod
{
    public static string PatchId => "CGSLinkedRewardClaim";

    public static ModPatchTarget[] GetTargets()
    {
        var target = typeof(NLinkedRewardSet);
        if (AccessTools.DeclaredMethod(target, "Reload", Type.EmptyTypes)?.ReturnType == typeof(void))
            return [new(target, "Reload", Type.EmptyTypes)];

        Entry.Logger.Warn("[Linked rewards] UI patch skipped. Expected void NLinkedRewardSet.Reload().");
        return [];
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var getReward = AccessTools.DeclaredMethod(typeof(NLinkedRewardSet), "GetReward", Type.EmptyTypes);
        if (getReward?.ReturnType != typeof(void))
        {
            Entry.Logger.Warn("[Linked rewards] UI patch skipped. Expected void NLinkedRewardSet.GetReward().");
            return code;
        }

        var matches = FindClaimCallbacks(code, getReward);
        if (matches.Count != 1)
        {
            Entry.Logger.Warn(
                $"[Linked rewards] UI patch skipped. Expected one GetReward() → Action → Callable.From(Action) binding in NLinkedRewardSet.Reload(), found {matches.Count}.");
            return code;
        }

        matches[0].operand = AccessTools.DeclaredMethod(
            typeof(CGSLinkedRewardClaimPatch), nameof(CreateClaimCallback), [typeof(Action)]);
        return code;
    }

    private static List<CodeInstruction> FindClaimCallbacks(IEnumerable<CodeInstruction> code, MethodInfo getReward)
    {
        var actionConstructor = AccessTools.Constructor(typeof(Action), [typeof(object), typeof(IntPtr)]);
        var fromAction = AccessTools.DeclaredMethod(typeof(Callable), nameof(Callable.From), [typeof(Action)]);
        var meaningful = code.Where(instruction => instruction.opcode != OpCodes.Nop).ToArray();
        var matches = new List<CodeInstruction>();
        for (int i = 2; i < meaningful.Length; i++)
        {
            if (meaningful[i - 2].opcode == OpCodes.Ldftn && Equals(meaningful[i - 2].operand, getReward) &&
                meaningful[i - 1].opcode == OpCodes.Newobj && Equals(meaningful[i - 1].operand, actionConstructor) &&
                meaningful[i].opcode == OpCodes.Call && Equals(meaningful[i].operand, fromAction))
                matches.Add(meaningful[i]);
        }

        return matches;
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
