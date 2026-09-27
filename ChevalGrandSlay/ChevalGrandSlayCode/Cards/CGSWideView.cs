using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 宽阔视野
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSWideView : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("HandLimitBonus", 2m)
    ];

    public CGSWideView() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {

        await PowerCmd.Apply<CGSWideViewPower>(choiceContext, Owner.Creature,
            DynamicVars["HandLimitBonus"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["HandLimitBonus"].UpgradeValueBy(1m);
}
