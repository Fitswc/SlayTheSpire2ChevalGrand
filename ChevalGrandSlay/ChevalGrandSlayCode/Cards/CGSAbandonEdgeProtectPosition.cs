using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 弃锋护局
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSAbandonEdgeProtectPosition : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Uses", 2m), 
        new DynamicVar("MaxUses", 2m),
        new BlockVar(18m, ValueProp.Move)
    ];

    protected override bool IsPlayable => base.IsPlayable && Owner != null &&
        PileType.Hand.GetPile(Owner).Cards.Any(IsCandidate);

    public CGSAbandonEdgeProtectPosition() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
        
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);
        
        var selected = await CardSelectCmd.FromHand(
            choiceContext, 
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), 
            IsCandidate, 
            this
            );
        
        if (selected.FirstOrDefault() is not { } card)
        {
            return;
        }
        
        card.DynamicVars["Uses"].BaseValue = 0m;
        
        await CardPileCmd.Add(card, Entry.CGSFatePile);
        
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        DynamicVars.Block.UpgradeValueBy(6m);
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        DynamicVars["Uses"].IntValue <= 0 ? Entry.CGSFatePile : base.GetResultPileTypeForCardPlay();
    
    private bool IsCandidate(CardModel card) =>
        card != this && card.Type == CardType.Attack &&
        card.DynamicVars.ContainsKey("Uses") && card.DynamicVars["Uses"].IntValue > 0;
}
