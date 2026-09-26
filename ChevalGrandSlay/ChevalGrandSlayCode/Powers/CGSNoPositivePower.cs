using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSNoPositivePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => new();
    public override bool TryModifyPowerAmountReceived(PowerModel power, Creature target,
        decimal amount, Creature? applier, out decimal modified)
    {
        modified = target == Owner && power.Type == PowerType.Buff && amount > 0m ? 0m : amount;
        return modified != amount;
    }
}
