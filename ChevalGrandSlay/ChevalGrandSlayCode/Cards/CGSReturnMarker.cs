using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卡牌：回垒记号
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSReturnMarker : CGSStaminaCard
{
    // 基础耗能。
    private const int BaseEnergyCost = 0;

    // 卡牌类型。
    private const CardType CardKind = CardType.Skill;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Uncommon;

    // 目标类型（Self 表示自己）。
    private const TargetType CardTarget = TargetType.Self;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    public CGSReturnMarker() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CGSKeywords.Stamina];

    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar(CGSStamina.Uses, 4m),
        new DynamicVar(CGSStamina.MaxUses, 4m),
        new CardsVar(1),
        new DynamicVar("Discount", 1m)
    ];

    protected override async Task OnStaminaPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var cardPref = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        
        var selected = await CardSelectCmd.FromHand(
            choiceContext, 
            Owner,
            cardPref,
            selectedCard => selectedCard != this && CGSStamina.HasAtLeast(selectedCard, 2),
            this
            );

        if (selected.FirstOrDefault() is not { } card)
        {
            return;
        }
            
        CGSStamina.Spend(card, 1m);
        
        int discount = Math.Min(card.EnergyCost.GetWithModifiers(CostModifiers.All), DynamicVars["Discount"].IntValue);
        
        card.EnergyCost.AddThisTurn(-discount);
        
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        
    }

    protected override void OnUpgrade()
    {
        UpgradeStamina(1m);
        DynamicVars["Discount"].UpgradeValueBy(1m);
    }
}
