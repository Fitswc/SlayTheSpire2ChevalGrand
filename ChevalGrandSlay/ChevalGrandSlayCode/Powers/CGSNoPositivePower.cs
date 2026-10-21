using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSNoPositivePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    // 自定义图标路径。1:1即可。原版游戏大图256x256，小图64x64。
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/CGSNoPositivePower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/CGSNoPositivePower.png"
    );
    public override bool TryModifyPowerAmountReceived(PowerModel power, Creature target,
        decimal amount, Creature? applier, out decimal modified)
    {
        modified = target == Owner && power.Type == PowerType.Buff && amount > 0m ? 0m : amount;
        return modified != amount;
    }
}
