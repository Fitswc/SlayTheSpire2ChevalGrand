using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卡牌：独处的节奏

[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSSmoothBreathing : ModCardTemplate
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

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CGSKeywords.Stamina];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2),
        new DynamicVar("Uses", 2m),
        new DynamicVar("MaxUses", 2m)
    ];

    public CGSSmoothBreathing() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);

        await CardPileCmd.ShuffleIfNecessary(choiceContext, Owner);
        var drawPile = PileType.Draw.GetPile(Owner);
        var candidates = drawPile.Cards.Take(DynamicVars.Cards.IntValue).ToList();
        if (candidates.Count < 2)
        {
            await CardPileCmd.Draw(choiceContext, Owner);
            return;
        }

        var selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext, candidates, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1));
        var chosen = selected.SingleOrDefault();
        if (chosen == null)
        {
            return;
        }

        var originalTop = candidates[0];
        if (chosen != originalTop)
        {
            await CardPileCmd.Add(chosen, drawPile, CardPilePosition.Top);
        }

        var drawn = await CardPileCmd.Draw(choiceContext, Owner);
        if (drawn == null && chosen != originalTop)
        {
            await CardPileCmd.Add(originalTop, drawPile, CardPilePosition.Top);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        CGSConsumeUse.GetResultPileTypeForCardPlay(this, base.GetResultPileTypeForCardPlay());
}
