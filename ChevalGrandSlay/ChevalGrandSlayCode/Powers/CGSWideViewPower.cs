using STS2RitsuLib.Combat.HandSize;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSWideViewPower : ModPowerTemplate, IMaxHandSizeModifier
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    // 自定义图标路径。1:1即可。原版游戏大图256x256，小图64x64。
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/CGSWideViewPower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/CGSWideViewPower.png"
    );

    public override decimal ModifyHandDraw(Player player, decimal amount) =>
        player == Owner.Player ? amount + 1m : amount;

    public int ModifyMaxHandSize(Player player, int currentMaxHandSize) =>
        player == Owner.Player ? currentMaxHandSize + (int)Amount : currentMaxHandSize;
}
