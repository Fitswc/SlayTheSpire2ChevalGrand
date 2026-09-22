using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

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