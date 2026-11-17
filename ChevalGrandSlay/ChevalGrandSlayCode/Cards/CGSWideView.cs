using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卡牌：我憧憬的蓝

[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSWideView : ModCardTemplate
{
    // 基础耗能。
    private const int BaseEnergyCost = 2;

    // 卡牌类型。
    private const CardType CardKind = CardType.Power;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Rare;

    // 目标类型（Self 表示自己）。
    private const TargetType CardTarget = TargetType.Self;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [ModKeywordRegistry.CreateHoverTip(CGSKeywords.RestPile)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HandLimitBonus", 2m),
        new DynamicVar(CGSWideViewPower.RecoveryCountVar, CGSWideViewPower.BaseRecoveryCount),
        new DynamicVar(CGSWideViewPower.RecoveryAmountVar, CGSWideViewPower.BaseRecoveryAmount)
    ];

    public CGSWideView() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {

        var power = await PowerCmd.Apply<CGSWideViewPower>(choiceContext, Owner.Creature,
            DynamicVars["HandLimitBonus"].BaseValue, Owner.Creature, this);
        power?.SetRecovery(
            DynamicVars[CGSWideViewPower.RecoveryCountVar].IntValue,
            DynamicVars[CGSWideViewPower.RecoveryAmountVar].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[CGSWideViewPower.RecoveryCountVar].UpgradeValueBy(1m);
        DynamicVars[CGSWideViewPower.RecoveryAmountVar].UpgradeValueBy(1m);
    }
}
