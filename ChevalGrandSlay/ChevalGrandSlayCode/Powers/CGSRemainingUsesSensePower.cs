using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSRemainingUsesSensePower : ModPowerTemplate
{
    private int _triggeredTurn = -1;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    // 自定义图标路径。1:1即可。原版游戏大图256x256，小图64x64。
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/CGSRemainingUsesSensePower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/CGSRemainingUsesSensePower.png"
    );

    public override async Task AfterCardChangedPiles(CardModel card, PileType previousPile, AbstractModel? source)
    {
        if (card.Owner != Owner.Player || previousPile == Entry.CGSFatePile ||
            card.Pile?.Type != Entry.CGSFatePile || CombatState?.CurrentSide != CombatSide.Player ||
            Owner.Player?.PlayerCombatState is not { } state || _triggeredTurn == state.TurnNumber)
            return;
        _triggeredTurn = state.TurnNumber;
        await PlayerCmd.GainEnergy(Amount, Owner.Player);
    }
}
