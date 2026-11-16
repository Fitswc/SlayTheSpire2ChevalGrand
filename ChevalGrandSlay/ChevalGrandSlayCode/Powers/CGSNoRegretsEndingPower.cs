using ChevalGrandSlay.Keywords;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

// 能力：没跑完的话
[RegisterPower]
public sealed class CGSNoRegretsEndingPower : ModPowerTemplate
{
    private decimal _pendingBonus;
    private Dictionary<CardPlay, decimal> _attackBonuses = new(ReferenceEqualityComparer.Instance);
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModKeywordRegistry.CreateHoverTip(CGSKeywords.RestPile)
    ];
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;
    // 自定义图标路径。1:1即可。原版游戏大图256x256，小图64x64。
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/small/CGSNoRegretsEnding.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/CGSNoRegretsEnding.png"
    );

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _pendingBonus = 0m;
        _attackBonuses = new Dictionary<CardPlay, decimal>(ReferenceEqualityComparer.Instance);
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType previousPile, AbstractModel? source)
    {
        if (card.Owner == Owner.Player && previousPile != Entry.CGSFatePile &&
            card.Pile?.Type == Entry.CGSFatePile)
            _pendingBonus += Amount;

        return Task.CompletedTask;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (_pendingBonus <= 0m || card.Type != CardType.Attack || card.Owner != Owner.Player)
            return Task.CompletedTask;

        // Cards entering the Rest Pile during this attack charge the next attack.
        _attackBonuses.Add(cardPlay, _pendingBonus);
        _pendingBonus = 0m;

        return Task.CompletedTask;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal damage, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (dealer != Owner || cardSource?.Type != CardType.Attack || cardSource.Owner != Owner.Player ||
            !props.IsPoweredAttack())
            return 0m;

        // Damage previews have no CardPlay and show the bonus available to the next attack.
        if (cardPlay is null)
            return _pendingBonus;

        return ReferenceEquals(cardPlay.Card, cardSource) ? _attackBonuses.GetValueOrDefault(cardPlay) : 0m;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _attackBonuses.Remove(cardPlay);
        return Task.CompletedTask;
    }
}
