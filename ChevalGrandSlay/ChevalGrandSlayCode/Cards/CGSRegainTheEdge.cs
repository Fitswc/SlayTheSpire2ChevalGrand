using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 重振锋芒
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSRegainTheEdge : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7m, ValueProp.Move), new DynamicVar("Cleanse", 1m)];

    public CGSRegainTheEdge() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        for (int i = 0; i < DynamicVars["Cleanse"].IntValue; i++)
        {
            var debuff = Owner.Creature.Powers.FirstOrDefault(power =>
                power.Type == PowerType.Debuff && power.Amount > 0);
            if (debuff is null)
                break;
            await PowerCmd.ModifyAmount(choiceContext, debuff, -1m, Owner.Creature, this);
        }

        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this).Targeting(cardPlay.Target).Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["Cleanse"].UpgradeValueBy(1m);
    }
}
