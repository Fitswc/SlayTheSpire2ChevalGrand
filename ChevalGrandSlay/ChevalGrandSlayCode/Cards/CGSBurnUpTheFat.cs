using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
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
    }

    // 打出时的效果逻辑，这里是获得格挡。
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner.Creature;
        var ownedBlock = Owner.Creature.Block;

        // 清空当前格挡。
        if (creature.Block > 0)
        {
            await CreatureCmd.LoseBlock(creature, creature.Block);
        }
        
        // 退出防御姿态。
        if (creature.HasPower<CGSDefenseStancePower>())
        {
            await PowerCmd.Apply<CGSAccumulateStrength>(
                choiceContext,
                Owner.Creature,
                ownedBlock,
                Owner.Creature,
                this
            );

            //await PowerCmd.Remove<CGSDefenseStancePower>(creature);
            await PowerCmd.Apply<CGSAttackStancePower>(
                choiceContext, creature, 1m, creature, this);
        }
        
        else
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);

            await DamageCmd.Attack(ownedBlock)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }

        // 对自己施加虚弱。
        await PowerCmd.Apply<WeakPower>(
            choiceContext,
            creature,
            DynamicVars.Weak.BaseValue,
            creature,
            this);
    }

    // 升级后的效果逻辑。
    protected override void OnUpgrade()
    {
        DynamicVars.Weak.UpgradeValueBy(-2m);
    }
}