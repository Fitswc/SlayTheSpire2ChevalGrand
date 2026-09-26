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

// 弃垒争先
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSAbandonWallSeizeInitiative : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png"
        );
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(36m, ValueProp.Move)
    ];
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];
    
    protected override bool IsPlayable => base.IsPlayable && Owner is not null &&
        PileType.Hand.GetPile(Owner).Cards.Any(CGSLimitedCardTypeAndCount.IsSkill);
    
    public CGSAbandonWallSeizeInitiative() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true)
    {
        this.SecondaryResourceUses().Require("determination", CGSDetermination.CGSDeterminationId, 20);
    }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), CGSLimitedCardTypeAndCount.IsSkill, this);
        if (selected.FirstOrDefault() is not CGSLimitedUseCard card) return;
        card.ConsumeAllUses();
        await CardCmd.Exhaust(choiceContext, card);
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .Targeting(cardPlay.Target).Execute(choiceContext);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(12m);
}
