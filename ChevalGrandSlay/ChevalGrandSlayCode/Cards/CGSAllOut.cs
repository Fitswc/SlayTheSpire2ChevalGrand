using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 全力以赴
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSAllOut : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Uses", 2m),
        new DynamicVar("MaxUses", 2m)
    ];

    protected override bool IsPlayable => base.IsPlayable && Owner is not null &&
        PileType.Hand.GetPile(Owner).Cards.Any(IsCandidate);

    public CGSAllOut() : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);

        var selected = await CardSelectCmd.FromHand(
            choiceContext, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1), IsCandidate, this);
        if (selected.FirstOrDefault() is not { } card)
            return;

        // Fix the play count before the selected card consumes any of its uses.
        int playCount = card.DynamicVars["Uses"].IntValue;
        for (int i = 0; i < playCount; i++)
            await CardCmd.AutoPlay(choiceContext, card, null);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-2);
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        CGSConsumeUse.GetResultPileTypeForCardPlay(this, base.GetResultPileTypeForCardPlay());

    private bool IsCandidate(CardModel card) =>
        card != this && card.DynamicVars.ContainsKey("MaxUses") &&
        card.DynamicVars.TryGetValue("Uses", out var uses) && uses.IntValue > 0;
}
