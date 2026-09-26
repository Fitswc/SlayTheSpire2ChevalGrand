using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 借势卸困
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSBorrowForceEscape : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Reduction", 1m)];
    public CGSBorrowForceEscape() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true)
    {
        this.SecondaryResourceUses().Require("determination", CGSDetermination.CGSDeterminationId, 10);
    }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        await PowerCmd.Apply<CGSResistWeakFrailPower>(choiceContext, Owner.Creature,
            DynamicVars["Reduction"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars["Reduction"].UpgradeValueBy(1m);
}
