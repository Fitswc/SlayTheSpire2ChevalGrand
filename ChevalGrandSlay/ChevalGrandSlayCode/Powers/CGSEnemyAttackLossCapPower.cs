using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSEnemyAttackLossCapPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    // 自定义图标路径。1:1即可。原版游戏大图256x256，小图64x64。
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/CGSEnemyAttackLossCapPower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/CGSEnemyAttackLossCapPower.png"
    );
    private decimal _lost;

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal hpLoss, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || dealer?.IsEnemy != true || !props.IsPoweredAttack() ||
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
