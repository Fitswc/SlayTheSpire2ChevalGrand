using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 临阵决断
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSBattlefieldDecision : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    public CGSBattlefieldDecision() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        await PowerCmd.Apply<CGSDecisiveGuardPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
