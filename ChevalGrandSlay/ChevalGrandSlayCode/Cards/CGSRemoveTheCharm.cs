using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卸除护符
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSRemoveTheCharm : ModCardTemplate
{
    public override CardAssetProfile AssetProfile =>
        new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(4m, ValueProp.Move), new DynamicVar("ArtifactRemoved", 1m)];

    public CGSRemoveTheCharm() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var artifact = cardPlay.Target.Powers.OfType<ArtifactPower>().FirstOrDefault();
        if (artifact is not null)
        {
            decimal removed = Math.Min(Math.Max(0m, artifact.Amount), DynamicVars["ArtifactRemoved"].BaseValue);
            if (removed > 0m)
                await PowerCmd.ModifyAmount(choiceContext, artifact, -removed, Owner.Creature, this);
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(2)
            .FromCard(this).Targeting(cardPlay.Target).Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["ArtifactRemoved"].UpgradeValueBy(1m);
    }
}
