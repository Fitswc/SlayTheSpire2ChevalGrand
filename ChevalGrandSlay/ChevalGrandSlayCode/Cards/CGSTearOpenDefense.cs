using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 撕开防线
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSTearOpenDefense : ModCardTemplate
{
    private const string FirstAttackVulnerableVarName = "FirstAttackVulnerable";

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new PowerVar<VulnerablePower>(1m),
        new DynamicVar(FirstAttackVulnerableVarName, 1m)
    ];

    public CGSTearOpenDefense() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        bool isFirstAttack = CGSAttackChain.CountAttacksPlayedThisTurn(Owner, CombatState, cardPlay) == 0;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        decimal vulnerable = DynamicVars.Vulnerable.BaseValue;
        if (isFirstAttack)
            vulnerable += DynamicVars[FirstAttackVulnerableVarName].BaseValue;
        await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            cardPlay.Target,
            vulnerable,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars[FirstAttackVulnerableVarName].UpgradeValueBy(1m);
    }
}