using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 舍垒突进
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSAbandonBlockAdvance : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(9m, ValueProp.Move)];

    public CGSAbandonBlockAdvance() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        bool spentBlock = false;
        if (Owner.Creature.Block >= 4)
        {
            var choice = await CardSelectCmd.FromSimpleGrid(choiceContext, [this], Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 0, 1));
            spentBlock = choice.Any();
        }
        if (spentBlock)
            await CreatureCmd.LoseBlock(Owner.Creature, 4m);

        CGSNoRetaliationPower? protection = null;
        if (spentBlock)
            protection = await PowerCmd.Apply<CGSNoRetaliationPower>(
                choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        try
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this).Targeting(cardPlay.Target).Execute(choiceContext);
        }
        finally
        {
            if (protection is not null)
                await PowerCmd.Remove(protection);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
