using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 压住气焰
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSSuppressTheirSpirit : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8m, ValueProp.Move)];
    public CGSSuppressTheirSpirit() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true) { }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .Targeting(cardPlay.Target).Execute(choiceContext);
        await PowerCmd.Apply<CGSNoPositivePower>(choiceContext, cardPlay.Target, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
