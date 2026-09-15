using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public class CGSDefenseStancePower : ModPowerTemplate
{
    // 类型，Buff或Debuff
    public override PowerType Type => PowerType.Buff;
    // 叠加类型，Counter表示可叠加，Single表示不可叠加
    public override PowerStackType StackType => PowerStackType.Single;
    // 实例类型，默认会在已有的能力上堆叠。如果是Instanced，则每次都会新建一个实例。（像炸弹那样）
    // public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    // 自定义图标路径。1:1即可。原版游戏大图256x256，小图64x64。
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/test_power.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/test_power.png"
    );

    //格挡+2
    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (cardSource != null)
        {
            if (cardSource.Owner.Creature == Owner && props.IsPoweredCardOrMonsterMoveBlock())
            {
                return 2m;
            }
        }

        return 0m;
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy && Owner.Block > 0)
        {
            var remainingBlock = Owner.Block;
            // 将未消耗的格挡转化为姿态蓄力值
            await PowerCmd.Apply<CGSAccumulateStrength>(
                choiceContext,
                Owner,
                remainingBlock,
                Owner,
                null);
        }
    }
}
