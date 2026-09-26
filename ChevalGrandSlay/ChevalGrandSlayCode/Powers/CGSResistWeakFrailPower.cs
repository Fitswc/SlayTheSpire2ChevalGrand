using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSResistWeakFrailPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new();
    public override bool TryModifyPowerAmountReceived(PowerModel power, Creature target,
        decimal amount, Creature? applier, out decimal modified)
    {
        modified = target == Owner && amount > 0m &&
                   (power is WeakPower or FrailPower) ? Math.Max(0m, amount - Amount) : amount;
        return modified != amount;
    }
}
