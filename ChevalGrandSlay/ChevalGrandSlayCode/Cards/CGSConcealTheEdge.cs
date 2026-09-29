using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 收敛锋芒
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSConcealTheEdge : ModCardTemplate
{
    public override CardAssetProfile AssetProfile =>
        new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [
            new DynamicVar("Uses", 3m), 
            new DynamicVar("MaxUses", 3m),
            new DynamicVar("Transfer", 3m)
        ];

    public CGSConcealTheEdge() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
        
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);
        
        var strength = Owner.Creature.Powers.OfType<StrengthPower>().FirstOrDefault();
        decimal amount = Math.Min(Math.Max(0m, strength?.Amount ?? 0m), DynamicVars["Transfer"].BaseValue);
        if (amount == 0m || strength is null)
        {
            return;
        }
        await PowerCmd.ModifyAmount(choiceContext, strength, -amount, Owner.Creature, this);
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        DynamicVars["Transfer"].UpgradeValueBy(2m);
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        CGSConsumeUse.GetResultPileTypeForCardPlay(this, base.GetResultPileTypeForCardPlay());

}