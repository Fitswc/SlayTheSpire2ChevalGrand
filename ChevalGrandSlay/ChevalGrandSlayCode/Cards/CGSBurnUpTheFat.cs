using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

//TODO: Unfinished, Need to add the mechanism of FatalBlow
//TODO: Need to verify

namespace ChevalGrandSlay.Cards;

// 防御牌和打击一样注册到角色卡池，并作为 4 张初始卡加入角色卡组。
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSBurnUpTheFat : ModCardTemplate
{
    // 基础耗能。
    private const int BaseEnergyCost = 1;

    // 卡牌类型。
    private const CardType CardKind = CardType.Skill;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Uncommon;

    // 目标类型（Self 表示自己）。
    private const TargetType CardTarget = TargetType.AllEnemies;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;
    

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WeakPower>(6m)
    ];

    // 卡图资源。
    // 如果你按这行代码写，文件名就对应 ChevalGrandSlay/images/cards/ChevalGrandSlayDefend.png。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.None };

    public CGSBurnUpTheFat() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
        // 必须支付 1 点，不足时无法打出。
        this.SecondaryResourceUses().Require(
            "determination_required",
            CGSDetermination.CGSDeterminationId,
            1);

        // 在必需支付之外，按每份 1 点继续消耗剩余决意。
        this.SecondaryResourceUses().SpendExtra(
            "determination_extra",
            CGSDetermination.CGSDeterminationId,
            perStackAmount: 1,
            maxStacks: null);
    }

    // 打出时的效果逻辑，这里是获得格挡。
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        int spent = cardPlay.SecondaryResources()
            .Spent(CGSDetermination.CGSDeterminationId);

        if (spent > 0)
        {
            await CreatureCmd.GainBlock(
                Owner.Creature,
                spent,
                ValueProp.Unpowered,
                cardPlay);
        }

        await PowerCmd.Apply<WeakPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars.Weak.BaseValue,
            Owner.Creature,
            this);
    }

    // 升级后的效果逻辑。
    protected override void OnUpgrade()
    {
        DynamicVars.Weak.UpgradeValueBy(-2m);
    }
}