using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 折返标记
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSReturnMarker : ModCardTemplate
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromKeyword(CGSKeywords.Stamina)
    ];

    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Uses", 3m), 
        new DynamicVar("MaxUses", 3m), 
        new CardsVar(1),
        new DynamicVar("Discount", 1m)
    ];

    public CGSReturnMarker() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
        
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);

        var cardPref = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        
        var selected = await CardSelectCmd.FromHand(
            choiceContext, 
            Owner,
            cardPref,
            selectedCard => selectedCard != this && selectedCard.DynamicVars.ContainsKey("Uses") && selectedCard.DynamicVars["Uses"].IntValue >= 2,
            this
            );

        if (selected.FirstOrDefault() is not { } card)
        {
            return;
        }
            
        card.DynamicVars["Uses"].BaseValue -= 1m;
        
        int discount = Math.Min(card.EnergyCost.GetWithModifiers(CostModifiers.All), DynamicVars["Discount"].IntValue);
        
        card.EnergyCost.AddThisTurn(-discount);
        
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        DynamicVars["Discount"].UpgradeValueBy(1m);
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        CGSConsumeUse.GetResultPileTypeForCardPlay(this, base.GetResultPileTypeForCardPlay());
}
