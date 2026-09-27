using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
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
        this.SecondaryResourceUses().Require(
            "MinimumDetermination",
            CGSDetermination.CGSDeterminationId,
            1);
        this.SecondaryResourceUses().SpendExtra(
            "AllDetermination",
            CGSDetermination.CGSDeterminationId,
            perStackAmount: 1,
            maxStacks: null);
    }

    // 打出时的效果逻辑，这里是获得格挡。
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {

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

        // 将本次支付的决意存入生成牌，避免支付后读取到 0。
        int spent = cardPlay.SecondaryResources().Spent(CGSDetermination.CGSDeterminationId);
        var fatalBlow = (CGSFatalBlow)ModelDb.Card<CGSFatalBlow>().MutableClone();
        fatalBlow.SetDamage(spent);
        await CardPileCmd.AddGeneratedCardToCombat(fatalBlow, PileType.Hand, Owner);
    }

    // 升级后的效果逻辑。
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}