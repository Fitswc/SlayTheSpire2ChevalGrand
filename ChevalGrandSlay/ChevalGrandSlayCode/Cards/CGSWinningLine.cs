using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 胜势一线
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSWinningLine : CGSLimitedUseCard
{
    private const string BonusDamageVarName = "BonusDamage";

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move),
        new DynamicVar(BonusDamageVarName, 4m),
        new DynamicVar(UsesVarName, 6m)
    ];

    public CGSWinningLine() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        this.SecondaryResourceUses().Require("determination", CGSDetermination.CGSDeterminationId, 10);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        int previousAttacks = Math.Min(4, CGSAttackChain.CountAttacksPlayedThisTurn(Owner, CombatState, cardPlay));
        decimal damage = DynamicVars.Damage.BaseValue +
                         previousAttacks * DynamicVars[BonusDamageVarName].BaseValue;

        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
        ConsumeUse(cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars[BonusDamageVarName].UpgradeValueBy(1m);
        EnergyCost.UpgradeBy(-1);
    }
}