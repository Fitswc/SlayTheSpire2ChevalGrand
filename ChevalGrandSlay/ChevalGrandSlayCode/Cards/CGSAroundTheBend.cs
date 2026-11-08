using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卡牌：转角后的蓝
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSAroundTheBend : ModCardTemplate
{
    // 基础耗能。
    private const int BaseEnergyCost = 1;

    // 卡牌类型。
    private const CardType CardKind = CardType.Skill;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Common;

    // 目标类型（Self 表示自己）。
    private const TargetType CardTarget = TargetType.Self;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromKeyword(CGSKeywords.Stamina)
    ];

    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new CardsVar(1)
    ];

    public CGSAroundTheBend() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        
        var candidates = PileType.Hand.GetPile(Owner).Cards
            .Where(card => card != this && card.DynamicVars.ContainsKey("Uses") && !card.EnergyCost.CostsX)
            .ToList();
        
        if (candidates.Count == 0)
        {
            return;
        }

        var prefCard = new CardSelectorPrefs(SelectionScreenPrompt, 0, 1);
        
        var selected = await CardSelectCmd.FromHand(
            choiceContext,
            Owner, 
            prefCard, 
            candidates.Contains, 
            this
            );
        
        selected.FirstOrDefault()?.EnergyCost.AddThisTurn(-1);
        
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }

}
