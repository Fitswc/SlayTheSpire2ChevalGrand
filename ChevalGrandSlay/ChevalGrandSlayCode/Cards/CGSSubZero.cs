using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

//稳固步调

namespace ChevalGrandSlay.Cards;

[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSSubZero : ModCardTemplate
{
    // 基础耗能。
    private const int BaseEnergyCost = 2;

    // 卡牌类型。
    private const CardType CardKind = CardType.Power;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Rare;

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
        new BlockVar(9m, ValueProp.Move),
        new DynamicVar("EndTurnBlock", 9m)
    ];

    public CGSSubZero() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑，这里是获得格挡。
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.Creature.HasPower<CGSDefenseStancePower>())
        {
            await PowerCmd.Apply<CGSSubZeroPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["EndTurnBlock"].BaseValue,
                Owner.Creature,
                this);
        }
        else
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        }
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);

        var isDefense = false;
        var isAttack = false;

        // 图鉴中的原型以及尚未分配拥有者的卡牌显示完整说明。
        if (IsMutable && Owner != null)
        {
            if (Owner.Creature.HasPower<CGSDefenseStancePower>())
            {
                isDefense = true;
            }
            else if (Owner.Creature.HasPower<CGSAttackStancePower>())
            {
                isAttack = true;
            }
        }

        description.Add("IsDefense", isDefense);
        description.Add("IsAttack", isAttack);
    }

    // 升级后的效果逻辑。
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars["EndTurnBlock"].UpgradeValueBy(3m);
        EnergyCost.UpgradeBy(-1);
    }
}
