using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 再续胜负
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSRenewTheContest : ModCardTemplate
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromKeyword(CGSKeywords.Stamina)
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new DynamicVar("Uses", 2m),
            new DynamicVar("MaxUses", 2m), 
            new DynamicVar("Restore", 1m)
        ];

    //protected override bool IsPlayable => base.IsPlayable && GetCandidates().Count != 0;

    public CGSRenewTheContest() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self, true)
    {
        
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);
        
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, GetCandidates(), Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1));
        if (selected.FirstOrDefault() is not { } card) return;
        var uses = card.DynamicVars["Uses"];
        uses.BaseValue = Math.Min(card.DynamicVars["MaxUses"].BaseValue,
            uses.BaseValue + DynamicVars["Restore"].BaseValue);
        await CardPileCmd.Add(card, PileType.Hand);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        DynamicVars["Restore"].UpgradeValueBy(1m);
    }

    private List<CardModel> GetCandidates()
    {
        if (Owner == null)
            return [];

        return Entry.CGSFatePile.GetPile(Owner).Cards.Where(IsCandidate).ToList();
    }

    private static bool IsCandidate(CardModel card)
    {
        return card.Type == CardType.Attack && card.DynamicVars.ContainsKey("Uses");
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        CGSConsumeUse.GetResultPileTypeForCardPlay(this, base.GetResultPileTypeForCardPlay());

}
