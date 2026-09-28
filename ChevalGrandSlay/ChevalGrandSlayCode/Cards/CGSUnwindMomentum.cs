using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卸下冲势
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSUnwindMomentum : ModCardTemplate
{
    private const string DescriptionKey = "CHEVAL_GRAND_SLAY_CARD_CGS_UNWIND_MOMENTUM";

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(5m, ValueProp.Move), new PowerVar<WeakPower>(3m), new DynamicVar("HitReduction", 4m)];

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);

        var effect = new LocString("cards", IsUpgraded
            ? $"{DescriptionKey}.upgradedEffect"
            : $"{DescriptionKey}.baseEffect");
        effect.Add(IsUpgraded ? DynamicVars["HitReduction"] : DynamicVars.Weak);
        description.Add("Effect", effect);
    }

    public CGSUnwindMomentum() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (IsUpgraded)
            await PowerCmd.Apply<CGSNextEnemyHitReductionPower>(
                choiceContext, Owner.Creature, DynamicVars["HitReduction"].BaseValue, Owner.Creature, this);
        else if (cardPlay.Target?.Monster?.IntendsToAttack == true)
            await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target,
                DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);
}
