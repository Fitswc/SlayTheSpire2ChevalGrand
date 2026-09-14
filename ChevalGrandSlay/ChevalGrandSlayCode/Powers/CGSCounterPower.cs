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
public sealed class CGSCounterPower : ModPowerTemplate
{
    // 类型，Buff或Debuff
    public override PowerType Type => PowerType.Buff;

    // 叠加类型，Counter表示可叠加，Single表示不可叠加
    public override PowerStackType StackType => PowerStackType.Single;
    // 实例类型，默认会在已有的能力上堆叠。如果是Instanced，则每次都会新建一个实例。（像炸弹那样）
//     public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    // 自定义图标路径。1:1即可。原版游戏大图256x256，小图64x64。
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/test_power.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/test_power.png"
    );

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner // 受到伤害的是自己
            && result.BlockedDamage > 0 // 确实用格挡吸收了伤害
            && props.IsPoweredAttack() // 伤害属于攻击
            && Owner.IsPlayer) // 此写法用于玩家的 Power
        {
            // 获取当前可以受到攻击的敌人。
            var enemies = CombatState.HittableEnemies;

            if (enemies.Count > 0)
            {
                // 使用游戏的随机数系统选一个敌人。
                var enemy = Owner.Player?.RunState.Rng.CombatTargets
                    .NextItem(enemies);

                Flash();

                if (enemy != null)
                {
                    await CreatureCmd.Damage(
                        choiceContext,
                        enemy,
                        4m,
                        ValueProp.Unpowered, // 效果伤害，不吃力量等攻击加成
                        Owner,
                        null
                    );
                    
                    // 这个 Power 只触发一次，反击结算后将自己移除。
                    await PowerCmd.Remove(this);
                }
            }
        }
    }
}
