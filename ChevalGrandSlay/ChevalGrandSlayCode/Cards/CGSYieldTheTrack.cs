using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 让出跑道
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSYieldTheTrack : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [
            new DynamicVar("MinimumCost", 2m)
        ];
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => 
        [
            CardKeyword.Exhaust
        ];
    
    protected override bool IsPlayable => base.IsPlayable && Owner is not null &&
        PileType.Hand.GetPile(Owner).Cards.Any(IsCandidate);

    public CGSYieldTheTrack() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self, true)
    {
        
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = await CardSelectCmd.FromHandForDiscard(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), IsCandidate, this);
        if (selected.FirstOrDefault() is not { } card)
        {
            return;
        }
        
        await CardCmd.Discard(choiceContext, card);
        await PlayerCmd.GainEnergy(1m, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MinimumCost"].UpgradeValueBy(-1m); 
    }
    
    private bool IsCandidate(CardModel card) => card != this && !card.EnergyCost.CostsX &&
        card.EnergyCost.GetWithModifiers(CostModifiers.All) >= DynamicVars["MinimumCost"].IntValue;
}
