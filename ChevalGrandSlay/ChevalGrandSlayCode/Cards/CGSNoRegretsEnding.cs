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

// 无悔落幕
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSNoRegretsEnding : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new();
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public CGSNoRegretsEnding() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CGSNoRegretsEndingPower>(choiceContext, Owner.Creature,
            IsUpgraded ? 12m : 8m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {

    }
}

[RegisterPower]
public sealed class CGSNoRegretsEndingPower : ModPowerTemplate
{
    private int _triggeredTurn = -1;
    private bool _boostNextAttack;
    private CardModel? _triggeringCard;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new();

    public override Task AfterCardChangedPiles(CardModel card, PileType previousPile, AbstractModel? source)
    {
        if (card.Owner == Owner.Player && card.Type == CardType.Attack &&
            card.DynamicVars.ContainsKey("Uses") && previousPile != Entry.CGSFatePile &&
            card.Pile?.Type == Entry.CGSFatePile &&
            Owner.Player?.PlayerCombatState is { } state && _triggeredTurn != state.TurnNumber)
        {
            _triggeredTurn = state.TurnNumber;
            _boostNextAttack = true;
            _triggeringCard = card;
        }
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal damage, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (_boostNextAttack && cardSource?.Type == CardType.Attack && cardSource.Owner == Owner.Player)
            return Amount;
        return 0m;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_boostNextAttack && cardPlay.Card != _triggeringCard &&
            cardPlay.Card.Type == CardType.Attack && cardPlay.Card.Owner == Owner.Player)
            _boostNextAttack = false;
        return Task.CompletedTask;
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player && participants.Contains(Owner))
        {
            _boostNextAttack = false;
            _triggeringCard = null;
        }
        return Task.CompletedTask;
    }
}
