using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 再续胜负
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSRenewTheContest : ModCardTemplate
{
    private const string DeterminationCostVarName = "DeterminationCost";

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(DeterminationCostVarName, 12m),
        new CardsVar(1)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override bool IsPlayable => base.IsPlayable && GetCandidates().Count != 0;

    public CGSRenewTheContest() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self, true)
    {
        this.SecondaryResourceUses().Require("CGSDetermination", CGSDetermination.CGSDeterminationId, 12);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            IsCandidate,
            this);
        foreach (var card in selected.OfType<CGSLimitedUseCard>())
            card.RestoreUses(2);

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[DeterminationCostVarName].UpgradeValueBy(-3m);
        this.SecondaryResourceUses().Require("CGSDetermination", CGSDetermination.CGSDeterminationId, 9);
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        this.SecondaryResourceUses().Require("CGSDetermination", CGSDetermination.CGSDeterminationId, 12);
    }

    private List<CardModel> GetCandidates()
    {
        if (Owner == null)
            return [];

        return PileType.Hand.GetPile(Owner).Cards.Where(IsCandidate).ToList();
    }

    private static bool IsCandidate(CardModel card)
    {
        return card is CGSLimitedUseCard limitedUseCard &&
               limitedUseCard.Type == CardType.Attack &&
               limitedUseCard.RemainingUses < limitedUseCard.MaximumUses;
    }
}