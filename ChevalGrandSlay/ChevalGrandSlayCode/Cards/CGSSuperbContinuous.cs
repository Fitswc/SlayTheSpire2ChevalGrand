using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 防御牌和打击一样注册到角色卡池，并作为 4 张初始卡加入角色卡组。
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSSuperbContinuous : ModCardTemplate
{
    // 基础耗能。
    private const int BaseEnergyCost = 1;

    // 卡牌类型。
    private const CardType CardKind = CardType.Power;

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
    
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);

        description.Add(
            "DeterminationIcon",
            SecondaryResourceText.GetIconTag(
                CGSDetermination.CGSDeterminationId));
    }

    public CGSSuperbContinuous() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
        
    }

    // 打出时的效果逻辑，这里是获得格挡。
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        int currentDetermination = SecondaryResourceCmd.Get(
            Owner, CGSDetermination.CGSDeterminationId);

        await SecondaryResourceCmd.Spend(
            Owner,
            CGSDetermination.CGSDeterminationId,
            currentDetermination);

        // 只有升级后的卡牌才赋予群攻效果。
        if (IsUpgraded)
        {
            await PowerCmd.Apply<CGSSuperbContinuousPower>(
                choiceContext,
                Owner.Creature,
                1m,
                Owner.Creature,
                this);
        }

        // 无论是否升级，都生成一张 FatalBlow。
        await CardPileCmd.AddToCombatAndPreview<CGSFatalBlow>(
            Owner.Creature,
            PileType.Hand,
            1,
            Owner);
    }

    // 升级后的效果逻辑。
    protected override void OnUpgrade()
    {
       EnergyCost.UpgradeBy(-1);
    }
}