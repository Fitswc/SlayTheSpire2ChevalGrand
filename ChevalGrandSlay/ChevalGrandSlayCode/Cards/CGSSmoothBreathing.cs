using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Combat.HandSize;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卡牌：独处的节奏

[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSSmoothBreathing : CGSStaminaCard
{
    // 基础耗能。
    private const int BaseEnergyCost = 0;

    // 卡牌类型。
    private const CardType CardKind = CardType.Skill;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Uncommon;

    // 目标类型（Self 表示自己）。
    private const TargetType CardTarget = TargetType.Self;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    public CGSSmoothBreathing() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget)
    {
    }

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CGSKeywords.Stamina];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ModCardVars.Cards(4),
        new DynamicVar("SelectCount", 1m),
        new DynamicVar(CGSStamina.Uses, 4m),
        new DynamicVar(CGSStamina.MaxUses, 4m)
    ];

    protected override async Task OnStaminaPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.ShuffleIfNecessary(choiceContext, Owner);
        var drawPile = PileType.Draw.GetPile(Owner);
        var candidates = drawPile.Cards.Take(DynamicVars.Cards.IntValue).ToList();
        var hand = PileType.Hand.GetPile(Owner);
        int availableSlots = MaxHandSizeCalculator.Calculate(Owner) - hand.Cards.Count;
        int selectCount = Math.Min(DynamicVars["SelectCount"].IntValue, Math.Min(candidates.Count, availableSlots));
        if (selectCount <= 0)
            return;

        var selected = selectCount == candidates.Count
            ? candidates
            : await CardSelectCmd.FromSimpleGrid(
                choiceContext, candidates, Owner, new CardSelectorPrefs(SelectionScreenPrompt, selectCount));
        await CardPileCmd.Add(selected, hand);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["SelectCount"].UpgradeValueBy(1m);
    }
}
