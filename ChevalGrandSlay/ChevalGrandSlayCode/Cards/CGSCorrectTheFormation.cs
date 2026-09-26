using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 校正队列
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSCorrectTheFormation : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Look", 4m)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public CGSCorrectTheFormation() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
        this.SecondaryResourceUses().Require("determination", CGSDetermination.CGSDeterminationId, 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var cards = PileType.Draw.GetPile(Owner).Cards.Take(DynamicVars["Look"].IntValue).ToList();
        int minimum = IsUpgraded ? 0 : Math.Min(2, cards.Count);
        if (cards.Count == 0) return;
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, cards, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, minimum, Math.Min(2, cards.Count)));
        foreach (var card in cards.Where(selected.Contains))
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Bottom);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Look"].UpgradeValueBy(2m);
        this.SecondaryResourceUses().Require("determination", CGSDetermination.CGSDeterminationId, 0);
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        this.SecondaryResourceUses().Require("determination", CGSDetermination.CGSDeterminationId, 2);
    }
}
