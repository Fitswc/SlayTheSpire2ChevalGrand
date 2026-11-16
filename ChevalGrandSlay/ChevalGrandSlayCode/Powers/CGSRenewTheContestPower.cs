using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSRenewTheContestPower : ModPowerTemplate
{
    private HashSet<CardModel> _pendingCards = new(ReferenceEqualityComparer.Instance);
    private HashSet<CardPlay> _boostedPlays = new(ReferenceEqualityComparer.Instance);

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/small/CGSNoRegretsEnding.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/CGSNoRegretsEnding.png");

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _pendingCards = new(ReferenceEqualityComparer.Instance);
        _boostedPlays = new(ReferenceEqualityComparer.Instance);
    }

    internal void BoostNextPlay(CardModel card) => _pendingCards.Add(card);

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (_pendingCards.Remove(cardPlay.Card))
            _boostedPlays.Add(cardPlay);

        return Task.CompletedTask;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal damage, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (dealer != Owner || cardSource is null || !props.IsPoweredAttack())
            return 0m;

        // A preview has no CardPlay. Actual damage must belong to the marked play.
        bool boosted = cardPlay is null
            ? _pendingCards.Contains(cardSource)
            : ReferenceEquals(cardPlay.Card, cardSource) && _boostedPlays.Contains(cardPlay);
        return boosted ? Amount : 0m;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_boostedPlays.Remove(cardPlay) && _pendingCards.Count == 0 && _boostedPlays.Count == 0)
            await PowerCmd.Remove(this);
    }
}
