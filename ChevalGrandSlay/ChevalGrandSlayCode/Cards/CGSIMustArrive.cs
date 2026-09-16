using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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

    public CGSIMustArrive() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑，这里是获得格挡。
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner.Creature;
        var charge = creature.GetPowerAmount<CGSAccumulateStrength>();

        // 增加与现有蓄力相同的层数，实现翻倍。
        if (charge > 0)
        {
            await PowerCmd.Apply<CGSAccumulateStrength>(
                choiceContext,
                creature,
                charge,
                creature,
                this);
        }
        
        /*
        // 移除防御姿态，避免两种姿态同时存在。
        if (creature.HasPower<CGSDefenseStancePower>())
        {
            await PowerCmd.Remove<CGSDefenseStancePower>(creature);
        }
        */

        // 已经处于进攻姿态时，无须重复施加。
        if (!creature.HasPower<CGSAttackStancePower>())
        {
            await PowerCmd.Apply<CGSAttackStancePower>(
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
       EnergyCost.UpgradeBy(-1);
    }
}