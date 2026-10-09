using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 乘隙追击
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSOpportunisticPursuit : ModCardTemplate
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromKeyword(CGSKeywords.Stamina)
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Uses", 3m), 
        new DynamicVar("MaxUses", 3m),
        new DamageVar(6m, ValueProp.Move),
        new DynamicVar("BonusDamage", 5m),
    ];

    public CGSOpportunisticPursuit() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true)
    {
        
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        
        bool hasPreviousAttack = CGSAttackChain.CountAttacksPlayedThisTurn(Owner, CombatState, cardPlay) >= 1;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this).Targeting(cardPlay.Target).Execute(choiceContext);

        if (hasPreviousAttack)
        {
            await DamageCmd.Attack(DynamicVars["BonusDamage"].BaseValue)
                .FromCard(this).Targeting(cardPlay.Target).Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["BonusDamage"].UpgradeValueBy(2m);
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        CGSConsumeUse.GetResultPileTypeForCardPlay(this, base.GetResultPileTypeForCardPlay());

}
