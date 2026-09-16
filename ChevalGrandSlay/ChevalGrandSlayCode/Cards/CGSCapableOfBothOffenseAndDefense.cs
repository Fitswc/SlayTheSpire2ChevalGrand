using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 防御牌和打击一样注册到角色卡池，并作为 4 张初始卡加入角色卡组。
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSCapableOfBothOffenseAndDefense : ModCardTemplate
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
    
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Charge", 6m)
    ];

    // 卡图资源。
    // 如果你按这行代码写，文件名就对应 ChevalGrandSlay/images/cards/ChevalGrandSlayDefend.png。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.None };

    public CGSCapableOfBothOffenseAndDefense() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑，这里是获得格挡。
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner.Creature;

        if (creature.HasPower<CGSDefenseStancePower>())
        {
            var charge = creature.GetPowerAmount<CGSAccumulateStrength>();

            if (charge > 0)
            {
                await CreatureCmd.GainBlock(
                    creature,
                    charge,
                    ValueProp.Unpowered,
                    cardPlay);
            }
        }
        else if (creature.HasPower<CGSAttackStancePower>())
        {
            await PowerCmd.Apply<CGSAccumulateStrength>(
                choiceContext,
                creature,
                DynamicVars["Charge"].BaseValue,
                creature,
                this);

            await PowerCmd.Remove<CGSAttackStancePower>(creature);

            await PowerCmd.Apply<CGSDefenseStancePower>(
                choiceContext,
                creature,
                1m,
                creature,
                this);
        }
    }

    // 升级后的效果逻辑。
    protected override void OnUpgrade()
    {
        DynamicVars["Charge"].UpgradeValueBy(3m);
    }
}