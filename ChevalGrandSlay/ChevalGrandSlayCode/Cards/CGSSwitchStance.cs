using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

[RegisterCard(typeof(CGSCardPool))]
[RegisterCharacterStarterCard(typeof(CGSCharacter), 1)]
public sealed class CGSSwitchStance : ModCardTemplate
{
    // 基础耗能。
    private const int BaseEnergyCost = 0;

    // 卡牌类型。
    private const CardType CardKind = CardType.Skill;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Basic;

    // 目标类型
    private const TargetType CardTarget = TargetType.Self;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2)
    ];
    
    // 固有：战斗开始时，这张牌会进入起始手牌。
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Innate
    ];

    // 卡图资源。
    // 如果你按这行代码写，文件名就对应 ChevalGrandSlay/images/cards/ChevalGrandSlayStrike.png。
    // 这里的 res://ChevalGrandSlay/... 是 Godot 资源路径，对应的是你的资源文件夹名字。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.None };

    public CGSSwitchStance() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
        
    }

    // 打出时的效果逻辑。
    // 尖塔2使用了 async 和 await 来控制效果逻辑顺序执行，和尖塔1的 action 类似。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.Creature.HasPower<CGSDefenseStancePower>())
        {
            await PowerCmd.Remove<CGSDefenseStancePower>(Owner.Creature);
            await PowerCmd.Apply<CGSAttackStancePower>(
                choiceContext,
                Owner.Creature,
                1m,
                Owner.Creature,
                this
            );
        }

        else if (Owner.Creature.HasPower<CGSAttackStancePower>())
        {
            await PowerCmd.Remove<CGSAttackStancePower>(Owner.Creature);
            await PowerCmd.Apply<CGSDefenseStancePower>(
                choiceContext,
                Owner.Creature,
                1m,
                Owner.Creature,
                this
            );
        }

        var drawCards = await CardPileCmd.Draw(
            choiceContext,
            DynamicVars.Cards.BaseValue,
            Owner
        );
        
        var selectedCards = await CardSelectCmd.FromHandForDiscard(
            choiceContext,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1),
            card => drawCards.Contains(card),
            this
        );
        
        var selectedCard = selectedCards.FirstOrDefault();

        if (selectedCard != null)
        {
            await CardCmd.Discard(choiceContext, selectedCard);
        }
    }
    
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);

        var isDefense = false;
        var isAttack = false;

        // 图鉴中的卡牌可能没有拥有者，需要先检查。
        if (IsMutable)
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

        // 把检查结果传给本地化。
        description.Add("IsDefense", isDefense);
        description.Add("IsAttack", isAttack);
    }

    // 升级后的效果逻辑。
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}