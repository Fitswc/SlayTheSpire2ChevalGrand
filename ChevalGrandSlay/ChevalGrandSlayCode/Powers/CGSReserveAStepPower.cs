using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSReserveAStepPower : ModPowerTemplate
{
    private CardModel? _card;
    private int _turn;
    private bool _used;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override PowerAssetProfile AssetProfile => new();
    protected override bool IsVisibleInternal => false;

    public void SetCard(CardModel card, int turn)
    {
        _card = card;
        _turn = turn;
        _used = false;
    }

    public bool TryPreserve(CardModel card)
    {
        if (_used || _card != card || Owner.Player?.PlayerCombatState?.TurnNumber != _turn)
            return false;
        _used = true;
        return true;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player && participants.Contains(Owner))
            await PowerCmd.Remove(this);
    }
}
