using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Powers;
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

// 连胜步调
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSWinningRhythm : ModCardTemplate
{
    private const string PowerDamageVarName = "PowerDamage";

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(PowerDamageVarName, 3m)];

    public CGSWinningRhythm() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CGSWinningRhythmPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[PowerDamageVarName].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[PowerDamageVarName].UpgradeValueBy(1m);
    }
}
