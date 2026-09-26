using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSNoRetaliationPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => new();
    protected override bool IsVisibleInternal => false;

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal hpLoss, ValueProp props,
        Creature dealer, CardModel? cardSource)
    {
        return target == Owner && dealer.IsEnemy && CombatState?.CurrentSide == CombatSide.Player &&
               !props.IsPoweredAttack() ? 0m : hpLoss;
    }
}
