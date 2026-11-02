using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSOneBeatAheadPower : ModPowerTemplate
{
    private int _extraTurnNumber = -1;

    public bool IsExtraTurnActive => _extraTurnNumber >= 0;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/CGSOneBeatAheadPower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/CGSOneBeatAheadPower.png");

    public override bool ShouldTakeExtraTurn(Player player) =>
        player == Owner.Player && !IsExtraTurnActive;

    public override Task AfterTakingExtraTurn(Player player)
    {
        if (player == Owner.Player && !IsExtraTurnActive)
        {
            _extraTurnNumber = player.PlayerCombatState!.TurnNumber;
        }
        return Task.CompletedTask;
    }

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        // 保留至额外回合判定结束，不能在回合结束钩子中提前移除封锁。
        if (IsExtraTurnActive && (side != CombatSide.Player || !participants.Contains(Owner) ||
            Owner.Player?.PlayerCombatState?.TurnNumber != _extraTurnNumber))
            await PowerCmd.Remove(this);
    }
}
