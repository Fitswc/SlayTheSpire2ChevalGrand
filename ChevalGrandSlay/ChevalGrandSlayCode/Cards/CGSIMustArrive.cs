using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 防御牌和打击一样注册到角色卡池，并作为 4 张初始卡加入角色卡组。
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSIMustArrive : ModCardTemplate
{
    // 基础耗能。
    private const int BaseEnergyCost = 1;

    // 卡牌类型。
    private const CardType CardKind = CardType.Skill;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Rare;

    // 目标类型（Self 表示自己）。
    private const TargetType CardTarget = TargetType.Self;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    // 卡图资源。
    // 如果你按这行代码写，文件名就对应 ChevalGrandSlay/images/cards/ChevalGrandSlayDefend.png。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.None };
    
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    public CGSIMustArrive() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑，这里是获得格挡。
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner.Creature;
        int determination = SecondaryResourceCmd.Get(
            Owner, CGSDetermination.CGSDeterminationId);

        // 增加与当前决意相同的数量，实现翻倍。
        await SecondaryResourceCmd.Gain(
            Owner,
            CGSDetermination.CGSDeterminationId,
            determination,
            this);
    }

    // 升级后的效果逻辑。
    protected override void OnUpgrade()
    {
       EnergyCost.UpgradeBy(-1);
    }
}
