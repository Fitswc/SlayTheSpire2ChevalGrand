using ChevalGrandSlay.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSAttackStancePower : ModPowerTemplate
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

    //伤害+2
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner || cardSource?.Type != CardType.Attack || !props.IsPoweredAttack())
        {
            return 0m;
        }

        return 2m;
    }
    
    public override async Task AfterApplied(
        Creature? applier, CardModel? cardSource)
    {
        if (Owner.HasPower<CGSDefenseStancePower>() && Owner.Player is { } player)
        {
            // 确认是防御转进攻，由进攻姿态统一移除防御姿态。
            await PowerCmd.Remove<CGSDefenseStancePower>(Owner);

            // 生成一张牌加入手牌。
            await CardPileCmd.AddToCombatAndPreview<CGSFatalBlow>(Owner, PileType.Hand, 1, player);

            // 清除指定能力。
            if (Owner.HasPower<CGSAccumulateStrength>())
            {
                await PowerCmd.Remove<CGSAccumulateStrength>(Owner);
            }
        }
    }
}