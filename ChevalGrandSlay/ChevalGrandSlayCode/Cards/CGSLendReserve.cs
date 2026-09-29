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

// 转借余裕
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSLendReserve : ModCardTemplate
{
    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [
            new DynamicVar("Uses", 3m), 
            new DynamicVar("MaxUses", 3m), 
            new CardsVar(1),
            new DynamicVar("Restore", 1m)
        ];

    protected override bool IsPlayable => base.IsPlayable && Owner is not null &&
        PileType.Hand.GetPile(Owner).Cards.Count(card => card != this && card.DynamicVars.ContainsKey("Uses")) >= 2;
    
    public CGSLendReserve() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
        
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);

        var firstChoice = await CardSelectCmd.FromHand(
            choiceContext, 
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            card => card != this && card.DynamicVars.ContainsKey("Uses") && card.DynamicVars["Uses"].IntValue > 0, 
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
            card => card != this && card != first && card.DynamicVars.ContainsKey("Uses"), 
            this
            );
        
        if (secondChoice.FirstOrDefault() is not { } second)
        {
            return;
        }
        
        first.DynamicVars["Uses"].BaseValue -= 1m;

        if (first.DynamicVars["Uses"].IntValue == 0)
        {
            await CardPileCmd.Add(first, Entry.CGSFatePile);
        }
        var uses = second.DynamicVars["Uses"];
        uses.BaseValue = Math.Min(second.DynamicVars["MaxUses"].BaseValue, uses.BaseValue + DynamicVars["Restore"].BaseValue);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        DynamicVars["Restore"].UpgradeValueBy(1m);
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        CGSConsumeUse.GetResultPileTypeForCardPlay(this, base.GetResultPileTypeForCardPlay());

}

