using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Combat;
using ChevalGrandSlay.Mechanics;
using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 行云流水
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSFlowingAssault : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(1)];

    public CGSFlowingAssault() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CGSFlowingAssaultPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars.Cards.BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

[RegisterPower]
public sealed class CGSFlowingAssaultPower : ModPowerTemplate
{
    private int _triggeredTurn = -1;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new();

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner.Player;
        if (player?.PlayerCombatState is not { } state ||
            CombatState?.CurrentSide != CombatSide.Player ||
            cardPlay.Card.Owner != player ||
            cardPlay.Card.Type != CardType.Attack ||
            _triggeredTurn == state.TurnNumber ||
            CGSAttackChain.CountAttacksPlayedThisTurn(player, CombatState) < 3)
            return;

        _triggeredTurn = state.TurnNumber;
        await CardPileCmd.Draw(choiceContext, Amount, player);
    }
}
