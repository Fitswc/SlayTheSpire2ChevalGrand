using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 终线回声
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSFinishLineEcho : ModCardTemplate
{
    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [
            new DynamicVar("EchoDamage", 5m),
            new DynamicVar("Turns", 2m)
        ];

    public CGSFinishLineEcho() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true)
    {
        
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var power = await PowerCmd.Apply<CGSFinishLineEchoPower>(choiceContext, Owner.Creature,
            DynamicVars["EchoDamage"].BaseValue, Owner.Creature, this);
//        power?.SetTurns(IsUpgraded ? 3 : 2); 
        power?.SetTurns(DynamicVars["Turns"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["EchoDamage"].UpgradeValueBy(3m);
    }
}

