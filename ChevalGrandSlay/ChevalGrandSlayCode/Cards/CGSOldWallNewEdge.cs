using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 旧垒开新锋
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSOldWallNewEdge : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8m, ValueProp.Move), new DynamicVar("BlockSpent", 12m)];
    public CGSOldWallNewEdge() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true)
    {
        this.SecondaryResourceUses().Require("determination", CGSDetermination.CGSDeterminationId, 8);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        decimal spent = 0m;
        if (Owner.Creature.Block > 0)
        {
            var choice = await CardSelectCmd.FromSimpleGrid(choiceContext, [this], Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 0, 1));
            if (choice.Any())
                spent = Math.Min(Owner.Creature.Block, DynamicVars["BlockSpent"].BaseValue);
        }
        if (spent > 0) await CreatureCmd.LoseBlock(Owner.Creature, spent);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue + spent)
            .FromCard(this).Targeting(cardPlay.Target).Execute(choiceContext);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this).Targeting(cardPlay.Target).Execute(choiceContext);
    }

    protected override void OnUpgrade() { }
}
