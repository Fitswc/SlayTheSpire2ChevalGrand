using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Combat.HandSize;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;


// 卡牌：心中的终点
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSAimForTheFinish : CGSStaminaCard
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

    public CGSAimForTheFinish() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CGSKeywords.Stamina];

    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(CGSStamina.Uses, 4m),
        new DynamicVar(CGSStamina.MaxUses, 4m),
        new DynamicVar("SelectionCount", 1m)
    ];

    protected override async Task OnStaminaPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var candidates = PileType.Draw.GetPile(Owner).Cards
            .Concat(PileType.Discard.GetPile(Owner).Cards).ToList();

        var hand = PileType.Hand.GetPile(Owner);
        int availableSlots = MaxHandSizeCalculator.Calculate(Owner) - hand.Cards.Count;
        int selectionCount = Math.Min(DynamicVars["SelectionCount"].IntValue, Math.Min(candidates.Count, availableSlots));
        if (selectionCount <= 0)
            return;

        var selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, selectionCount));

        foreach (var card in selected)
        {
            if ((await CardPileCmd.Add(card, PileType.Hand)).success && card.Pile == hand)
                card.EnergyCost.AddThisTurn(-1);
        }
    }

    protected override void OnUpgrade() => DynamicVars["SelectionCount"].UpgradeValueBy(1m);
}
