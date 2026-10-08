using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 扫去杂念
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSClearDistractions : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    
    //protected override bool IsPlayable => base.IsPlayable && Owner is not null &&
    //    PileType.Hand.GetPile(Owner).Cards.Any(IsCandidate);

    public CGSClearDistractions() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), IsCandidate, this);
        if (selected.FirstOrDefault() is not { } card)
        {
            return;
        }
        
        await CardCmd.Exhaust(choiceContext, card);
        await CardPileCmd.Draw(choiceContext, 1m, Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
    
    private static bool IsCandidate(CardModel card) => card.Type is CardType.Status or CardType.Curse;
}
