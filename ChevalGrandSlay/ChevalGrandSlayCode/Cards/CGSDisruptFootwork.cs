using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 打乱步点
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSDisruptFootwork : CGSLimitedUseCard
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WeakPower>(6m),
        new DynamicVar(UsesVarName, 4m)
    ];

    public CGSDisruptFootwork() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        this.SecondaryResourceUses().Require("determination", CGSDetermination.CGSDeterminationId, 14);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState is not { } combatState)
            return;

        foreach (var enemy in combatState.Enemies.Where(enemy => enemy.IsAlive))
        {
            await PowerCmd.Apply<WeakPower>(
                choiceContext,
                enemy,
                DynamicVars.Weak.BaseValue,
                Owner.Creature,
                this);
        }

        ConsumeUse(cardPlay);
    }

    protected override void OnUpgrade()
    {
    }
}