using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSFatalBlow : ModCardTemplate
{
    // 基础耗能。
    private const int BaseEnergyCost = 0;

    // 卡牌类型。
    private const CardType CardKind = CardType.Attack;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Token;

    // 构造时的默认目标；实际目标由下方属性根据能力决定。
    private const TargetType CardTarget = TargetType.AnyEnemy;
    public override TargetType TargetType
    {
        get
        {
            if (IsMutable && Owner != null && Owner.Creature.HasPower<CGSSuperbContinuousPower>())
            {
                return TargetType.AllEnemies;
            }

            return TargetType.AnyEnemy;
        }
    }

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = false;
    
    public override int MaxUpgradeLevel => 0;

    // 卡图资源。
    // 这里的 res://ChevalGrandSlay/... 是 Godot 资源路径，对应的是你的资源文件夹名字。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    // CanonicalVars 翻译是“规范值”，指卡牌的基础数值。
    // 添加一个 DamageVar 意为指定卡牌的基础伤害是多少；它会自动绑定到本地化里的 {Damage:diff()} 占位符。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ModCardVars.Computed("BlowValue", 0, card =>
        {
            decimal damage = 0m;
            if (card != null)
            {
                damage = card.DynamicVars["BlowValue"].BaseValue;
                if (card.IsMutable && card.Owner != null)
                {
                    damage += SecondaryResourceCmd.Get(
                        card.Owner, CGSDetermination.CGSDeterminationId);
                }
            }

            return damage;
        }),
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust,
        CardKeyword.Retain
    ];

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };

    public CGSFatalBlow() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑。
    // 尖塔2使用了 async 和 await 来控制效果逻辑顺序执行，和尖塔1的 action 类似。
    // DamageCmd.Attack 会按当前 DynamicVars.Damage 的值造成攻击伤害。
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var damage = ((ComputedDynamicVar)DynamicVars["BlowValue"]).Calculate();

        if (Owner.Creature.HasPower<CGSSuperbContinuousPower>())
        {
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .TargetingAllOpponents(CombatState)
                .Execute(choiceContext);
        }
        else
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);

            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }
    }
}
