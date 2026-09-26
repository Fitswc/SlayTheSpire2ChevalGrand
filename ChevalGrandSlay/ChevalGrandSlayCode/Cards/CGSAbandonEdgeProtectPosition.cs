using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.SecondaryResources;
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
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(36m, ValueProp.Move)
    ];
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];
    
    protected override bool IsPlayable => base.IsPlayable && Owner != null &&
        PileType.Hand.GetPile(Owner).Cards.Any(CGSLimitedCardTypeAndCount.IsAttack);
    
    public CGSAbandonEdgeProtectPosition() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
        this.SecondaryResourceUses().Require("determination", CGSDetermination.CGSDeterminationId, 20);
    }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), CGSLimitedCardTypeAndCount.IsAttack, this);
        if (selected.FirstOrDefault() is not CGSLimitedUseCard card) return;
        card.ConsumeAllUses();
        await CardCmd.Exhaust(choiceContext, card);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(12m);
}
