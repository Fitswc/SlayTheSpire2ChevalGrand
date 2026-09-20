using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 沉重踏步
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSHeavyStep : ModCardTemplate
{
    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new DynamicVar("SpendLimit", 4m),
        ModCardVars.ComputedDamage("TotalDamage", 8m, card =>
        {
            if (card == null)
                return 8m;

            // 与资源费用图标使用同一支付计划，包含消耗上限和支付修正。
            int spent = SecondaryResourcePaymentResolver.Plan(card).Lines
                .Where(line => line.ResourceId == CGSDetermination.CGSDeterminationId)
                .Sum(line => line.AmountToSpend);
            return card.DynamicVars.Damage.BaseValue + 2m * spent;
        }, ValueProp.Move)
    ];

    public CGSHeavyStep() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true)
    {
        this.SecondaryResourceUses().SpendExtra("determination", CGSDetermination.CGSDeterminationId, 1, 4);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int spent = cardPlay.SecondaryResources().Spent(CGSDetermination.CGSDeterminationId);
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue + 2m * spent)
            .FromCard(this).Targeting(cardPlay.Target).Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Damage"].UpgradeValueBy(2m);
        DynamicVars["SpendLimit"].UpgradeValueBy(1m);
        this.SecondaryResourceUses().SpendExtra("determination", CGSDetermination.CGSDeterminationId, 1, 5);
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        this.SecondaryResourceUses().SpendExtra("determination", CGSDetermination.CGSDeterminationId, 1, 4);
    }
}
