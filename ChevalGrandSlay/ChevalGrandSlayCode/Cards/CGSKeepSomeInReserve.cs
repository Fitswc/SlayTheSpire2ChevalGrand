using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using ChevalGrandSlay.Powers;

namespace ChevalGrandSlay.Cards;

// 留有余裕
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSKeepSomeInReserve : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("RetainEnergy", 1m)];

    public CGSKeepSomeInReserve() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        await PowerCmd.Apply<CGSEnergyReservePower>(choiceContext, Owner.Creature,
            DynamicVars["RetainEnergy"].BaseValue, Owner.Creature, this);

    protected override void OnUpgrade() => DynamicVars["RetainEnergy"].UpgradeValueBy(1m);
}
