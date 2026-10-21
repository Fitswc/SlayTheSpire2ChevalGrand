using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Combat;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSForgeAheadPower : ModPowerTemplate
{
    private int _triggeredTurn = -1;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    // 自定义图标路径。1:1即可。原版游戏大图256x256，小图64x64。
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/CGSForgeAheadPower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/CGSForgeAheadPower.png"
    );

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
        Flash();
        await PlayerCmd.GainEnergy(1m, player);
    }
}
