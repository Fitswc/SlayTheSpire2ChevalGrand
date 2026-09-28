using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;


[RegisterPower]
public sealed class CGSFillTheGapPower : ModPowerTemplate
{
    private int _triggeredTurn = -1;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new();

    public override async Task AfterCardChangedPiles(CardModel card, PileType previousPile, AbstractModel? source)
    {
        if (card.Owner != Owner.Player || previousPile != Entry.CGSFatePile ||
            card.Pile?.Type == Entry.CGSFatePile || !card.DynamicVars.ContainsKey("Uses") ||
            CombatState?.CurrentSide != CombatSide.Player ||
            Owner.Player?.PlayerCombatState is not { } state || _triggeredTurn == state.TurnNumber)
            return;
        _triggeredTurn = state.TurnNumber;
        var uses = card.DynamicVars["Uses"];
        uses.BaseValue = Math.Min(card.DynamicVars["MaxUses"].BaseValue, uses.BaseValue + 1m);
        if (Amount > 1m) await PlayerCmd.GainEnergy(1m, Owner.Player);
    }
}

