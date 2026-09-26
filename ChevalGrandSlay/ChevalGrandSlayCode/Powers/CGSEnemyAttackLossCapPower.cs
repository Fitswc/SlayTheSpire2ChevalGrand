using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSEnemyAttackLossCapPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => new();
    private decimal _lost;
    public override decimal ModifyHpLostAfterOsty(Creature target, decimal hpLoss, ValueProp props,
        Creature dealer, CardModel? cardSource)
    {
        if (target != Owner || !dealer.IsEnemy || !props.IsPoweredAttack() ||
            CombatState?.CurrentSide != CombatSide.Enemy) return hpLoss;
        decimal allowed = Math.Max(0m, Amount - _lost);
        decimal actual = Math.Min(hpLoss, allowed);
        _lost += actual;
        return actual;
    }
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy) await PowerCmd.Remove(this);
    }
}
