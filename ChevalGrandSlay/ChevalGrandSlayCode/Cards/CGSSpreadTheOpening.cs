using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卡牌：浪纹扩散
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSSpreadTheOpening : ModCardTemplate
{
    // 基础耗能。
    private const int BaseEnergyCost = 0;

    // 卡牌类型。
    private const CardType CardKind = CardType.Skill;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Uncommon;

    // 目标类型（AllEnemies 表示所有敌人）。
    private const TargetType CardTarget = TargetType.AllEnemies;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    public override CardAssetProfile AssetProfile =>
        new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars => [ModCardVars.Power<VulnerablePower>(2m)];
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => 
        [
            CardKeyword.Exhaust
        ];

    public CGSSpreadTheOpening() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState ?? throw new InvalidOperationException("The card requires an active combat.");

        foreach (var enemy in combatState.HittableEnemies.ToArray())
            await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, DynamicVars.Vulnerable.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Vulnerable.UpgradeValueBy(2m);
}
