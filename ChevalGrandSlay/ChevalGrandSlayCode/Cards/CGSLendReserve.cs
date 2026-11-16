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

// 卡牌：传球

[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSLendReserve : CGSStaminaCard
{
    // 基础耗能。
    private const int BaseEnergyCost = 1;

    // 卡牌类型。
    private const CardType CardKind = CardType.Skill;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Uncommon;

    // 目标类型（Self 表示自己）。
    private const TargetType CardTarget = TargetType.Self;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    public CGSLendReserve() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CGSKeywords.Stamina];

    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [
            new DynamicVar(CGSStamina.Uses, 4m),
            new DynamicVar(CGSStamina.MaxUses, 4m),
            new CardsVar(1),
            new DynamicVar("Restore", 1m)
        ];

    protected override async Task OnStaminaPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var firstChoice = await CardSelectCmd.FromHand(
            choiceContext, 
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            card => card != this && CGSStamina.HasAtLeast(card, 1),
            this
            );
        
        if (firstChoice.FirstOrDefault() is not { } first)
        {
            return;
        }
        
        var secondChoice = await CardSelectCmd.FromHand(
            choiceContext, 
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            card => card != this && card != first && CGSStamina.HasStamina(card),
            this
            );
        
        if (secondChoice.FirstOrDefault() is not { } second)
        {
            return;
        }
        
        await CGSStamina.SpendAndRestIfDepleted(first, 1m);
        CGSStamina.Restore(second, DynamicVars["Restore"].BaseValue);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        UpgradeStamina(1m);
        DynamicVars["Restore"].UpgradeValueBy(1m);
    }
}
