using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSDecisiveGuardPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => new();
    private int _cost;

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal hpLoss, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || dealer?.IsEnemy != true || !props.IsPoweredAttack() ||
            CombatState?.CurrentSide != CombatSide.Enemy || hpLoss <= 0m || _cost > 0)
            return hpLoss;
        int cost = (int)Math.Ceiling(hpLoss / 2m);
        if (SecondaryResourceCmd.Get(Owner.Player!, CGSDetermination.CGSDeterminationId) < cost)
            return hpLoss;
        _cost = cost;
        return 0m;
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (_cost <= 0 || target != Owner) return;
        await SecondaryResourceCmd.Spend(Owner.Player!, CGSDetermination.CGSDeterminationId, _cost);
        _cost = 0;
        await PowerCmd.Remove(this);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy) await PowerCmd.Remove(this);
    }
}