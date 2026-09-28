using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using ChevalGrandSlay.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// RegisterCard 会把这张牌交给 RitsuLib 自动注册。
// RegisterCharacterStarterCard 会把它追加进 ChevalGrandSlayCharacter 的初始卡组。
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSCounterAttack : ModCardTemplate
{
    // 基础耗能。
    private const int BaseEnergyCost = 1;

    // 卡牌类型。
    private const CardType CardKind = CardType.Skill;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Common;

    // 目标类型（AnyEnemy 表示任意敌人）。
    private const TargetType CardTarget = TargetType.AnyEnemy;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;


    // 卡图资源。
    // 如果你按这行代码写，文件名就对应 ChevalGrandSlay/images/cards/CGSForwardAStep.png。
    // 这里的 res://ChevalGrandSlay/... 是 Godot 资源路径，对应的是你的资源文件夹名字。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    // CanonicalVars 翻译是“规范值”，指卡牌的基础数值。
    // 添加一个 DamageVar 意为指定卡牌的基础伤害是多少；它会自动绑定到本地化里的 {Damage:diff()} 占位符。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("RandomAttack", 4m),
        new BlockVar(7m, ValueProp.Move),
    ];

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

    public CGSCounterAttack() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {

    }

    // 打出时的效果逻辑。
    // 尖塔2使用了 async 和 await 来控制效果逻辑顺序执行，和尖塔1的 action 类似。
    // DamageCmd.Attack 会按当前 DynamicVars.Damage 的值造成攻击伤害。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        await PowerCmd.Apply<CGSCounterAttackPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["RandomAttack"].BaseValue,
            Owner.Creature,
            this
        );

    }

    // 升级后的效果逻辑。
    protected override void OnUpgrade()
    {
        DynamicVars["RandomAttack"].UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}

[RegisterPower]
public sealed class CGSCounterAttackPower : ModPowerTemplate
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
                        Amount,
                        ValueProp.Unpowered,
                        Owner,
                        null
                    );

                }
            }
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy)
            await PowerCmd.Remove(this);
    }
}
