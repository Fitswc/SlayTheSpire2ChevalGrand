using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 弥补空白
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSFillTheGap : CGSStaminaCardTemplate
{
    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("PowerStack", 1m)
    ];

    public CGSFillTheGap() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CGSFillTheGapPower>(
            choiceContext, 
            Owner.Creature,
            DynamicVars["PowerStack"].BaseValue, 
            Owner.Creature, 
            this);
    }

    protected override void OnUpgrade()
    {
       DynamicVars["PowerStack"].UpgradeValueBy(1m);
    }
}
