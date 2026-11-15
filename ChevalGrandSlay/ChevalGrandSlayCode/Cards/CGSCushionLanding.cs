using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卡牌：小歇
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSCushionLanding : CGSStaminaCard
{
    // 基础耗能。
    private const int BaseEnergyCost = 1;

    // 卡牌类型。
    private const CardType CardKind = CardType.Skill;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Common;

    // 目标类型（Self 表示自己）。
    private const TargetType CardTarget = TargetType.Self;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    private int _fateEnteredTurn = -1;

    public CGSCushionLanding() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromKeyword(CGSKeywords.RestPile)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CGSKeywords.Stamina];

    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [
            new DynamicVar(CGSStamina.Uses, 3m),
            new DynamicVar(CGSStamina.MaxUses, 3m),
            new BlockVar(6m, ValueProp.Move)
        ];

    protected override async Task OnStaminaPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;

        await CreatureCmd.GainBlock(player.Creature, DynamicVars.Block, cardPlay);
        if (player.PlayerCombatState is { } state && _fateEnteredTurn == state.TurnNumber)
        {
            await CardPileCmd.Draw(choiceContext, 1m, player);
        }
    }

    protected override void OnUpgrade()
    {
        UpgradeStamina(1m);
        DynamicVars.Block.UpgradeValueBy(3m);
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType previousPile, AbstractModel? source)
    {
        if (Owner is { PlayerCombatState: { } state } player && card.Owner == player &&
            card.Pile?.Type == Entry.CGSFatePile && previousPile != Entry.CGSFatePile)
            _fateEnteredTurn = state.TurnNumber;
        return Task.CompletedTask;
    }
}
