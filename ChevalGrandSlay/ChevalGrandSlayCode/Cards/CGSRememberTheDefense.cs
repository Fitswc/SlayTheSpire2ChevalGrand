using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 守备铭记
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSRememberTheDefense : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Uses", 2m), new DynamicVar("MaxUses", 2m), new PowerVar<DexterityPower>(1m)];
    protected override bool IsPlayable => base.IsPlayable && Owner is not null &&
        PileType.Hand.GetPile(Owner).Cards.Any(IsCandidate);
    public CGSRememberTheDefense() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self, true)
    {
    }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);
        var selected = await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), IsCandidate, this);
        if (selected.FirstOrDefault() is not { } card) return;
        card.DynamicVars["Uses"].BaseValue = 0m;
        await CardPileCmd.Add(card, Entry.CGSFatePile);
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature,
            DynamicVars["DexterityPower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["DexterityPower"].UpgradeValueBy(1m);

    protected override PileType GetResultPileTypeForCardPlay() =>
        DynamicVars["Uses"].IntValue <= 0 ? Entry.CGSFatePile : base.GetResultPileTypeForCardPlay();


    private bool IsCandidate(CardModel card) =>
        card != this && card.Type == CardType.Skill && card.DeckVersion is not null &&
        (card.GainsBlock || card.DynamicVars.ContainsKey("Block")) &&
        card.DynamicVars.ContainsKey("Uses") &&
        card.DynamicVars["Uses"].IntValue > 0;
}
