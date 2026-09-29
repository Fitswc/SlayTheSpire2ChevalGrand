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

// 命运引航
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSFateGuidance : ModCardTemplate
{
    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new DynamicVar("Uses", 2m), 
        new DynamicVar("MaxUses", 2m), 
        new DynamicVar("Restore", 1m)
    ];

    public CGSFateGuidance() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self, true)
    {
        
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);

        var candidates = Entry.CGSFatePile.GetPile(Owner).Cards
            .Where(targetCard => targetCard.DynamicVars.ContainsKey("Uses"))
            .ToList();
        
        if (candidates.Count == 0)
        {
            return;
        }
        
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, candidates, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1));
        
        if (selected.FirstOrDefault() is not { } card)
        {
            return;
        }
        var uses = card.DynamicVars["Uses"];
        uses.BaseValue = Math.Min(card.DynamicVars["MaxUses"].BaseValue, uses.BaseValue + DynamicVars["Restore"].BaseValue);
        await CardPileCmd.Add(card, PileType.Hand);
        card.EnergyCost.AddThisTurn(-1);
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

