using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 轻重调配
//Verify
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSBalanceTheWeight : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => 
    [
        CardKeyword.Exhaust
    ];
    
    protected override bool IsPlayable => base.IsPlayable && Owner is not null &&
        PileType.Hand.GetPile(Owner).Cards.Count(card => card != this) >= 2 &&
        PileType.Hand.GetPile(Owner).Cards.Any(CanDiscount);

    public CGSBalanceTheWeight() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
        
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var discounted = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), CanDiscount, this)).FirstOrDefault();
        
        if (discounted is null)
        {
            return;
        }
        
        var increased = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), card => card != this && card != discounted, this))
            .FirstOrDefault();
        
        if (increased is null)
        {
            return;
        }

        int amount = 1;
        
        if (IsUpgraded)
        {
            // 选中0、1或2张牌，选中张数就是本次费用转移量。
            var amountChoice = await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                new[] { discounted, increased }, 
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 0, 2)
                );
            
            amount = amountChoice.Count();
        }
        
        amount = Math.Min(amount, discounted.EnergyCost.GetWithModifiers(CostModifiers.All));
        
        if (amount == 0)
        {
            return;
        }
        
        discounted.EnergyCost.AddThisTurn(-amount);
        increased.EnergyCost.AddThisTurn(amount);
    }

    protected override void OnUpgrade() { }

    private bool CanDiscount(CardModel card) => card != this && !card.EnergyCost.CostsX &&
        card.EnergyCost.GetWithModifiers(CostModifiers.All) > 0;
}
