using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卡牌：汗水会回应
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSCountTheCost : CGSStaminaCard
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

    private int _fateEnteredTurn = -1;

    public CGSCountTheCost() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget)
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
        new DynamicVar(CGSStamina.Uses, 4m),
        new DynamicVar(CGSStamina.MaxUses, 4m),
        ModCardVars.Cards(2)
    ];

    protected override async Task OnStaminaPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, player);
        if (player.PlayerCombatState is { } state && _fateEnteredTurn == state.TurnNumber)
        {
            await PlayerCmd.GainEnergy(2m, player);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType previousPile, AbstractModel? source)
    {
        if (Owner is { PlayerCombatState: { } state } player && card.Owner == player &&
            card.Pile?.Type == Entry.CGSFatePile && previousPile != Entry.CGSFatePile)
            _fateEnteredTurn = state.TurnNumber;
        return Task.CompletedTask;
    }
}
