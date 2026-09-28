using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

//分毫不让

namespace ChevalGrandSlay.Cards;

[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSNoConcession : ModCardTemplate
{
    // 基础耗能。
    private const int BaseEnergyCost = 1;

    // 卡牌类型。
    private const CardType CardKind = CardType.Skill;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Uncommon;

    // 目标类型（Self 表示自己）。
    private const TargetType CardTarget = TargetType.Self;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    public override bool GainsBlock => true;

    // 卡图资源。
    // 如果你按这行代码写，文件名就对应 ChevalGrandSlay/images/cards/ChevalGrandSlayReorganizeThePace.png。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

    // 卡牌基础数值。
    // BlockVar 会绑定到本地化里的 {Block:diff()}，升级时文本会自动显示差值。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6m, ValueProp.Move),
        new DynamicVar("CounterDamage", 9m), new DynamicVar("CounterCharges", 1m)
    ];

    public CGSNoConcession() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑，这里是获得格挡。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(
            Owner.Creature, DynamicVars.Block, cardPlay);

        await PowerCmd.Apply<CGSFullBlockCounterPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["CounterDamage"].BaseValue,
            Owner.Creature,
            this);
        Owner.Creature.Powers.OfType<CGSFullBlockCounterPower>().FirstOrDefault()
            ?.SetCharges(DynamicVars["CounterCharges"].IntValue);
    }

    // 升级后的效果逻辑。
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars["CounterDamage"].UpgradeValueBy(3m);
        DynamicVars["CounterCharges"].UpgradeValueBy(1m);
    }
}

[RegisterPower]
public sealed class CGSFullBlockCounterPower : ModPowerTemplate
{
    private int _charges = 1;
    public void SetCharges(int charges) => _charges = charges;
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

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (CombatState.CurrentSide != CombatSide.Enemy ||
            target != Owner ||
            !Owner.IsPlayer ||
            !props.IsPoweredAttack() ||
            result.BlockedDamage <= 0 ||
            !result.WasFullyBlocked)
            return;

        var owner = Owner;
        var enemies = CombatState.HittableEnemies.ToArray();

        var damage = Amount;
        _charges--;
        if (_charges <= 0) await PowerCmd.Remove(this);

        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive)
                continue;

            await CreatureCmd.Damage(
                choiceContext,
                enemy,
                damage,
                ValueProp.Unpowered,
                owner,
                null);
        }
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy)
            await PowerCmd.Remove(this);
    }
}
