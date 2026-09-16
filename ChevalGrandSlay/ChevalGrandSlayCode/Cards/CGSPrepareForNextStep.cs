using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSPrepareForNextStep : ModCardTemplate
{
    // 基础耗能。
    private const int BaseEnergyCost = 2;

    // 卡牌类型。
    private const CardType CardKind = CardType.Skill;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Uncommon;

    // 目标类型
    private const TargetType CardTarget = TargetType.Self;
    
    public override bool GainsBlock => true;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6m, ValueProp.Move),
        new BlockVar("BlockNextTurn", 10m, ValueProp.Move)
    ];
    
    // 卡图资源。
    // 如果你按这行代码写，文件名就对应 ChevalGrandSlay/images/cards/ChevalGrandSlayStrike.png。
    // 这里的 res://ChevalGrandSlay/... 是 Godot 资源路径，对应的是你的资源文件夹名字。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

    public CGSPrepareForNextStep() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
        
    }

    // 打出时的效果逻辑。
    // 尖塔2使用了 async 和 await 来控制效果逻辑顺序执行，和尖塔1的 action 类似。
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var nextBlock = (BlockVar)DynamicVars["BlockNextTurn"];

        // 按打出时的状态计算下回合格挡，包含敏捷、姿态等修正。
        var nextAmount = Hook.ModifyBlock(
            CombatState,
            Owner.Creature,
            nextBlock.BaseValue,
            nextBlock.Props,
            this,
            cardPlay,
            out _);

        await CreatureCmd.GainBlock(
            Owner.Creature, DynamicVars.Block, cardPlay);

        await PowerCmd.Apply<BlockNextTurnPower>(
            choiceContext,
            Owner.Creature,
            nextAmount,
            Owner.Creature,
            this);
    }

    // 升级后的效果逻辑。
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(3m);
        DynamicVars["BlockNextTurn"].UpgradeValueBy(5m);
        EnergyCost.UpgradeBy(-1);
    }
}