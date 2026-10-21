using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 偶遇旧迹
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSChanceReunion : ModCardTemplate
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromKeyword(CGSKeywords.RestPile)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CGSKeywords.Stamina];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Uses", 2m),
        new DynamicVar("MaxUses", 2m),
        new DynamicVar("RecoveryCount", 1m),
        new DynamicVar("Restore", 1m)
    ];

    public CGSChanceReunion() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);

        await CGSRandomRestRecovery.Recover(this, DynamicVars["RecoveryCount"].IntValue,
            DynamicVars["Restore"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["RecoveryCount"].UpgradeValueBy(1m);
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        CGSConsumeUse.GetResultPileTypeForCardPlay(this, base.GetResultPileTypeForCardPlay());
}
