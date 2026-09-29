using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 交接时机
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSHandoverMoment : ModCardTemplate
{
    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [
            new DynamicVar("Uses", 2m),
            new DynamicVar("MaxUses", 2m), 
            new CardsVar(2)
        ];

    public CGSHandoverMoment() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self, true)
    {
        
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);

        var selected = await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            selectedCard => selectedCard != this && selectedCard.DynamicVars.ContainsKey("Uses") && selectedCard.DynamicVars["Uses"].IntValue >= 2, this);
        if (selected.FirstOrDefault() is not { } card)
        {
            return;
        }
        
        var uses = card.DynamicVars["Uses"];
        uses.BaseValue -= 1m;

        if (uses.IntValue == 0)
        {
            await CardPileCmd.Add(card, Entry.CGSFatePile);
        }
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        CGSConsumeUse.GetResultPileTypeForCardPlay(this, base.GetResultPileTypeForCardPlay());



}

