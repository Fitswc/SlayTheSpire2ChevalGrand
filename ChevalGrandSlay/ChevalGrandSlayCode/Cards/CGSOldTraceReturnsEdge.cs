using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 旧迹回锋
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSOldTraceReturnsEdge : ModCardTemplate
{
    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new DynamicVar("Uses", 3m), 
        new DynamicVar("MaxUses", 3m), 
        new DamageVar(9m, ValueProp.Move),
        new DynamicVar("BonusBlock", 4m)
    ];

    public CGSOldTraceReturnsEdge() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true)
    {
        
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);

        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .Targeting(cardPlay.Target).Execute(choiceContext);
        
        if (Entry.CGSFatePile.GetPile(Owner).Cards.Count > 0)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["BonusBlock"].BaseValue, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["BonusBlock"].UpgradeValueBy(2m);
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        CGSConsumeUse.GetResultPileTypeForCardPlay(this, base.GetResultPileTypeForCardPlay());



}

