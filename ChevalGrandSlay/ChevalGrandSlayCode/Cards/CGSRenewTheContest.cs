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
        [new DynamicVar(DeterminationCostVarName, 12m)];

    protected override bool IsPlayable => base.IsPlayable && GetCandidates().Count > 0;

    public CGSRenewTheContest() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self, true)
    {
        this.SecondaryResourceUses().Require("CGSDetermination", CGSDetermination.CGSDeterminationId, 12);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            GetCandidates(),
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1));
        foreach (var card in selected.OfType<CGSLimitedUseCard>())
        {
            card.RestoreUse();
            await CardPileCmd.Add(card, PileType.Discard);
        }
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

        return Entry.CGSFatePile.GetPile(Owner).Cards
            .OfType<CGSLimitedUseCard>()
            .Where(card => card.Type == CardType.Attack && card.RemainingUses == 0)
            .Cast<CardModel>()
            .ToList();
    }
}