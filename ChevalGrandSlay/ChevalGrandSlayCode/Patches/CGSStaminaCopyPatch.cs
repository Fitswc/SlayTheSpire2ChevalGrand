using ChevalGrandSlay.Cards;
using ChevalGrandSlay.Compatibility;
using ChevalGrandSlay.Mechanics;
using ChevalGrandSlay.Relics;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace ChevalGrandSlay;

internal sealed class CGSStaminaCopyPatch : IPatchMethod
{
    public static string PatchId => "CGSStaminaCopy";

    // 在可变克隆创建后处理体力。原版克隆流程先运行，补丁随后补齐或替换其返回值。
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(AbstractModel), nameof(AbstractModel.MutableClone), Type.EmptyTypes)];

    public static void Postfix(AbstractModel __instance, ref AbstractModel __result)
    {
        if (__instance is not CardModel original || __result is not CardModel copy)
            return;

        // 只处理本模组的牌及兼容层生成牌。后者可能由 AutoAnthony 程序集定义。
        if (original is not ICGSGeneratedCard && original.GetType().Assembly != typeof(CGSStaminaCopyPatch).Assembly)
            return;

        if (!CGSStamina.HasStamina(original) || !CGSStamina.HasStamina(copy))
            return;

        // 原卡体力耗尽时，用归属同一玩家的空壳替换克隆结果。
        if (CGSStamina.IsDepleted(original))
        {
            var emptyShell = ModelDb.Card<CGSEmptyShell>().ToMutable();
            emptyShell.Owner = original.Owner;
            // 由调用方将空壳注册到对应的卡牌作用域。
            __result = emptyShell;
            return;
        }

        // 有体力的副本继承原卡的当前体力、上限和升级版起始遗物提供的体力加成。
        CGSStamina.Copy(original, copy);
        CGSStartRelicPlus.InheritStaminaBonus(original, copy);
    }
}
