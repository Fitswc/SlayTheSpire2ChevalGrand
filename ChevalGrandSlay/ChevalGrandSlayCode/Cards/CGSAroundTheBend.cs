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

// 穿过弯道
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSAroundTheBend : ModCardTemplate
{
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

    public CGSAroundTheBend() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self, true)
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
