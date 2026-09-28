using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 余次感知
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSRemainingUsesSense : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new();
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public CGSRemainingUsesSense() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CGSRemainingUsesSensePower>(choiceContext, Owner.Creature,
            IsUpgraded ? 2m : 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }


}

[RegisterPower]
public sealed class CGSRemainingUsesSensePower : ModPowerTemplate
{
    private int _triggeredTurn = -1;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new();

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

