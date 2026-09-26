using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 穿隙而入
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSPierceTheGap : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(8m, ValueProp.Move | ValueProp.Unblockable)];

    public CGSPierceTheGap() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await CreatureCmd.Damage(choiceContext, cardPlay.Target, DynamicVars.Damage.BaseValue,
            ValueProp.Move | ValueProp.Unblockable, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
