using ChevalGrandSlay.Utilities;
using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卡牌：坚定步伐

// RegisterCard 会把这张牌交给 RitsuLib 自动注册。
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSStepSteady : ModCardTemplate
{
    // 基础耗能。
    private const int BaseEnergyCost = 1;

    // 卡牌类型。
    private const CardType CardKind = CardType.Attack;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Common;

    // 目标类型（AllEnemies 表示所有敌人）。
    private const TargetType CardTarget = TargetType.AllEnemies;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    // 卡图资源。
    // 如果你按这行代码写，文件名就对应 ChevalGrandSlay/images/cards/ChevalGrandSlayStepSteady.png。
    // 这里的 res://ChevalGrandSlay/... 是 Godot 资源路径，对应的是你的资源文件夹名字。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    // CanonicalVars 翻译是“规范值”，指卡牌的基础数值。
    // 添加一个 DamageVar 意为指定卡牌的基础伤害是多少；它会自动绑定到本地化里的 {Damage:diff()} 占位符。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ModCardVars.Damage(8m, ValueProp.Move)
    ];

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };

    public CGSStepSteady() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal damage = Owner.Creature.Block;
        if (damage <= 0m || CombatState is not { } combatState)
            return;

        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay).CGSWithVfx("vfx/vfx_attack_slash")
            .TargetingAllOpponents(combatState)
            .Execute(choiceContext);
    }

    // 升级后的效果逻辑。
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}
