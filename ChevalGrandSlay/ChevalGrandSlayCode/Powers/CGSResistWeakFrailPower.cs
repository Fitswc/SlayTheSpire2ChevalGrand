using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;


// 能力：不惧不安
[RegisterPower]
public sealed class CGSResistWeakFrailPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    // 自定义图标路径。1:1即可。原版游戏大图256x256，小图64x64。
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/small/CGSResistWeakFrail.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/CGSResistWeakFrail.png"
    );

    public override bool TryModifyPowerAmountReceived(PowerModel power, Creature target,
        decimal amount, Creature? applier, out decimal modified)
    {
        modified = target == Owner && amount > 0m &&
            (power is WeakPower or FrailPower) ? Math.Max(0m, amount - Amount) : amount;
        return modified != amount;
    }
}
