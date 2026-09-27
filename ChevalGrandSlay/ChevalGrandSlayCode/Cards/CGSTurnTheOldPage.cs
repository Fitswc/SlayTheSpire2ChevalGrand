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

// 翻过旧页
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSTurnTheOldPage : ModCardTemplate
{
    public CGSTurnTheOldPage() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }
    public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override bool IsPlayable => base.IsPlayable && Owner is not null &&
                                          PileType.Hand.GetPile(Owner).Cards.Any(IsCandidate);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), IsCandidate, this);
        if (selected.FirstOrDefault() is not CGSLimitedUseCard card) return;
        card.SpendUses(2);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);

    private static bool IsCandidate(MegaCrit.Sts2.Core.Models.CardModel card) =>
        card is CGSLimitedUseCard { RemainingUses: >= 2 } && card.DeckVersion is not null;
}
