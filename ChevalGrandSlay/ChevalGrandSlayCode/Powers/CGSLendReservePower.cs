using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Combat.HandSize;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSLendReservePower : ModPowerTemplate, IMaxHandSizeModifier
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/small/CGSWideView.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/CGSWideView.png");

    public int ModifyMaxHandSize(Player player, int currentMaxHandSize) =>
        player == Owner.Player ? currentMaxHandSize + Amount : currentMaxHandSize;
}
