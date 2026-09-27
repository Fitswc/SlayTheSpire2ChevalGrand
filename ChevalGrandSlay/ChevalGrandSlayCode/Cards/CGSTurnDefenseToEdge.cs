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

// 化守为锋
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSTurnDefenseToEdge : ModCardTemplate
{
    public override CardAssetProfile AssetProfile =>
        new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Transfer", 3m)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public CGSTurnDefenseToEdge() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
        this.SecondaryResourceUses().Require("determination", CGSDetermination.CGSDeterminationId, 4);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var dexterity = Owner.Creature.Powers.OfType<DexterityPower>().FirstOrDefault();
        decimal amount = Math.Min(Math.Max(0m, dexterity?.Amount ?? 0m), DynamicVars["Transfer"].BaseValue);
        if (amount == 0m || dexterity is null) return;
        await PowerCmd.ModifyAmount(choiceContext, dexterity, -amount, Owner.Creature, this);
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Transfer"].UpgradeValueBy(2m);
}
