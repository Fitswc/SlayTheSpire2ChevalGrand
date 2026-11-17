using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卡牌：Tidebreaker
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSFateGuidance : CGSStaminaCard
{
    // 基础耗能。
    private const int BaseEnergyCost = 1;

    // 卡牌类型。
    private const CardType CardKind = CardType.Skill;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Rare;

    // 目标类型（Self 表示自己）。
    private const TargetType CardTarget = TargetType.Self;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;
    
    public override int MaxUpgradeLevel => 0;

    public CGSFateGuidance() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModKeywordRegistry.CreateHoverTip(CGSKeywords.RestPile)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CGSKeywords.Stamina];

    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new DynamicVar(CGSStamina.Uses, 2m),
        new DynamicVar(CGSStamina.MaxUses, 2m),
    ];

    protected override async Task OnStaminaPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var candidates = Entry.CGSFatePile.GetPile(Owner).Cards
            .Where(targetCard => CGSStamina.HasStamina(targetCard))
            .ToList();
        
        if (candidates.Count == 0)
        {
            return;
        }
        
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, candidates, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, Math.Min(4,candidates.Count)));
        
        if (selected.FirstOrDefault() is not { } card)
        {
            return;
        }
        CGSStamina.RestoreFull(card);
        await CardPileCmd.Add(card, PileType.Hand);
        card.EnergyCost.AddThisTurn(-1);
    }

    protected override void OnUpgrade()
    {
        
    }
}
