using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Combat;
using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 重步不乱
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSUnwaveringStep : ModCardTemplate
{
    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6m, ValueProp.Move)
    ];

    public CGSUnwaveringStep() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CGSUnwaveringStepPower>(choiceContext, Owner.Creature,
            DynamicVars.Block.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Block"].UpgradeValueBy(3m);
    }
}

[RegisterPower]
public sealed class CGSUnwaveringStepPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new();

    private int _triggeredTurn = -1;
    private CardPlay? _pendingPlay;

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        var player = Owner.Player;
        var card = cardPlay.Card;
        if (player?.PlayerCombatState is { } state && CombatState?.CurrentSide == CombatSide.Player &&
            card.Owner == player && card.Type == CardType.Attack && !card.EnergyCost.CostsX &&
            card.EnergyCost.GetWithModifiers(CostModifiers.None) >= 2 &&
            _triggeredTurn != state.TurnNumber)
        {
            // 先占用本回合的触发机会，等这张攻击牌结算完毕再获得格挡。
            _triggeredTurn = state.TurnNumber;
            _pendingPlay = cardPlay;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_pendingPlay != cardPlay)
            return;
        _pendingPlay = null;
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
    }
}
