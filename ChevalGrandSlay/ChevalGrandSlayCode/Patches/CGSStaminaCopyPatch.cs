using ChevalGrandSlay.Cards;
using ChevalGrandSlay.Mechanics;
using ChevalGrandSlay.Relics;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace ChevalGrandSlay;

internal sealed class CGSStaminaCopyPatch : IPatchMethod
{
    public static string PatchId => "stamina_copy";

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(AbstractModel), nameof(AbstractModel.MutableClone), Type.EmptyTypes)];

    public static void Postfix(AbstractModel __instance, ref AbstractModel __result)
    {
        if (__instance is not CardModel original || __result is not CardModel copy)
            return;

        // 只处理本模组的卡牌。
        if (original.GetType().Assembly != typeof(CGSStaminaCopyPatch).Assembly)
            return;

        // 原卡与副本都必须具有完整的耐力变量。
        if (!CGSStamina.HasStamina(original) || !CGSStamina.HasStamina(copy))
            return;

        // 耐力耗尽时，复制出来的是空卡牌，归属仍与原卡一致。
        if (CGSStamina.IsDepleted(original))
        {
            var emptyShell = ModelDb.Card<CGSEmptyShell>().ToMutable();
            emptyShell.Owner = original.Owner;
            // 副本仍由调用方注册到对应的卡牌作用域。
            __result = emptyShell;
            return;
        }

        // 完整保留当前耐力与上限，包括升级和战斗中的修改。
        CGSStamina.Copy(original, copy);
        CGSStartRelicPlus.InheritStaminaBonus(original, copy);
    }
}
