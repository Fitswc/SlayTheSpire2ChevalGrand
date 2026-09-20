using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

// 心中有数
[RegisterPower]
public sealed class CGSMeasuredMindPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new();

    private int _triggeredTurn = -1;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner.Player;
        if (player?.PlayerCombatState is not { } state || CombatState?.CurrentSide != CombatSide.Player ||
            cardPlay.Card.Owner != player || cardPlay.Card.Type != CardType.Attack ||
            _triggeredTurn == state.TurnNumber ||
            cardPlay.SecondaryResources().Spent(CGSDetermination.CGSDeterminationId) <= 0)
            return;
        // 抽牌可能再次触发打牌，先占用本回合的触发机会。
        _triggeredTurn = state.TurnNumber;
        await CardPileCmd.Draw(choiceContext, Amount, player);
    }
}
