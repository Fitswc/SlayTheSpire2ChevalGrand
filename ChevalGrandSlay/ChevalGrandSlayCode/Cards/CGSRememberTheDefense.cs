using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 守备铭记
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSRememberTheDefense : CGSLimitedUseCard
{
    public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(UsesVarName, 4m), new PowerVar<DexterityPower>(1m)];
    protected override bool IsPlayable => base.IsPlayable && Owner is not null &&
        PileType.Hand.GetPile(Owner).Cards.Any(CGSLimitedCardTypeAndCount.IsDefensiveSkill);
    public CGSRememberTheDefense() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self, true)
    {
        this.SecondaryResourceUses().Require("determination", CGSDetermination.CGSDeterminationId, 4);
    }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), CGSLimitedCardTypeAndCount.IsDefensiveSkill, this);
        if (selected.FirstOrDefault() is not CGSLimitedUseCard card) return;
        card.ConsumeAllUses();
        await CardCmd.Exhaust(choiceContext, card);
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature,
            DynamicVars["DexterityPower"].BaseValue, Owner.Creature, this);
        ConsumeUse(cardPlay);
    }
    protected override void OnUpgrade() => DynamicVars["DexterityPower"].UpgradeValueBy(1m);
}
