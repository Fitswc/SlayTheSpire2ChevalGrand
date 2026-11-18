using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卡牌：超大分汉堡肉

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class CGSEnergyDrink : ModCardTemplate
{
    private const int PlaysPerWeightGain = 6;

    // 基础耗能。
    private const int BaseEnergyCost = 0;

    // 卡牌类型。
    private const CardType CardKind = CardType.Skill;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.None;

    // 目标类型（Self 表示自己）。
    private const TargetType CardTarget = TargetType.Self;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    public override int MaxUpgradeLevel => 0;

    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<CGSWeightGain>()];

    public CGSEnergyDrink() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // CardPlayStarted is recorded before OnPlay, once for each play.
        int plays = CombatManager.Instance.History.CardPlaysStarted.Count(entry => entry.CardPlay.Card is CGSEnergyDrink && entry.CardPlay.Card.Owner == Owner);

        await CreatureCmd.Heal(Owner.Creature, 6m, false);

        if (plays > 0 && plays % PlaysPerWeightGain == 0 && CombatState is { } combatState)
        {
            var weightGain = combatState.CreateCard<CGSWeightGain>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(weightGain, PileType.Hand, Owner);
        }
    }
}
