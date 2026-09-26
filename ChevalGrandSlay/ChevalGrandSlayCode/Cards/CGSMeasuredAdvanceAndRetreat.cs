using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 进退有界
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSMeasuredAdvanceAndRetreat : CGSLimitedUseCard
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new DynamicVar(UsesVarName, 2m), 
            new DynamicVar("LossCap", 12m)
        ];
    
    public CGSMeasuredAdvanceAndRetreat() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self, true)
    {
        this.SecondaryResourceUses().Require("determination", CGSDetermination.CGSDeterminationId, 15);
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CGSEnemyAttackLossCapPower>(choiceContext, Owner.Creature,
            DynamicVars["LossCap"].BaseValue, Owner.Creature, this);
        ConsumeUse(cardPlay);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["LossCap"].UpgradeValueBy(-4m);
    }
}
