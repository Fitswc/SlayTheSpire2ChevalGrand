using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Combat.HandSize;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSWideViewPower : ModPowerTemplate, IMaxHandSizeModifier
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new();

    /*
    public override decimal ModifyHandDraw(Player player, decimal amount) =>
        player == Owner.Player ? amount + 1m : amount;
        */

    public int ModifyMaxHandSize(Player player, int currentMaxHandSize)
    {
        if (player == Owner.Player)
        {
            return currentMaxHandSize;
        }

        return currentMaxHandSize += 2;
    }
}
