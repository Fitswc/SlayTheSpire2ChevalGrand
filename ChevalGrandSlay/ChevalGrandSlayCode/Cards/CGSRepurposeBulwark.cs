using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 挪用壁垒
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSRepurposeBulwark : CGSStaminaCardTemplate
{
    public override bool GainsBlock => true;
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new DynamicVar("Uses", 3m), 
            new DynamicVar("MaxUses", 3m), 
            new DynamicVar("BlockLimit", 9m)
        ];

    public CGSRepurposeBulwark() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy, true)
    {
        
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        int removed = Math.Min(cardPlay.Target.Block, DynamicVars["BlockLimit"].IntValue);
        if (removed <= 0)
        {
            return;
        }

        await CreatureCmd.LoseBlock(cardPlay.Target, removed);
        await CreatureCmd.GainBlock(Owner.Creature, removed, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        DynamicVars["BlockLimit"].UpgradeValueBy(4m);
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        CGSConsumeUse.GetResultPileTypeForCardPlay(this, base.GetResultPileTypeForCardPlay());

}
