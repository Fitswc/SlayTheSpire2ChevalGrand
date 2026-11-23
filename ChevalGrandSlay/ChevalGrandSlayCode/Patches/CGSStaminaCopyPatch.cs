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

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(AbstractModel), nameof(AbstractModel.MutableClone), Type.EmptyTypes)];

    public static void Postfix(AbstractModel __instance, ref AbstractModel __result)
    {
        if (__instance is not CardModel original || __result is not CardModel copy)
            return;

        // Generated cards can belong to the optional AutoAnthony assembly.
        if (original is not ICGSGeneratedCard && original.GetType().Assembly != typeof(CGSStaminaCopyPatch).Assembly)
            return;

        if (!CGSStamina.HasStamina(original) || !CGSStamina.HasStamina(copy))
            return;

        if (CGSStamina.IsDepleted(original))
        {
            var emptyShell = ModelDb.Card<CGSEmptyShell>().ToMutable();
            emptyShell.Owner = original.Owner;
            // The caller registers the replacement in its card scope.
            __result = emptyShell;
            return;
        }

        CGSStamina.Copy(original, copy);
        CGSStartRelicPlus.InheritStaminaBonus(original, copy);
    }
}
