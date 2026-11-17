using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using ChevalGrandSlay.Utilities;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卡牌：脚步渐轻
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSChainStep : CGSStaminaCard
{
    // 基础耗能。
    private const int BaseEnergyCost = 1;

    // 卡牌类型。
    private const CardType CardKind = CardType.Attack;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Common;

    // 目标类型（AnyEnemy 表示任意敌人）。
    private const TargetType CardTarget = TargetType.AnyEnemy;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    private int _discountedTurn = -1;

    public CGSChainStep() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CGSKeywords.Stamina];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new DynamicVar(CGSStamina.Uses, 6m),
            new DynamicVar(CGSStamina.MaxUses, 6m),
            ModCardVars.Damage(10m, ValueProp.Move)
        ];

    protected override async Task OnStaminaPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay).CGSWithVfx("vfx/vfx_attack_slash").Targeting(cardPlay.Target).Execute(choiceContext);
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        RefreshDiscount(cardPlay);
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RefreshDiscount();
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
            _discountedTurn = -1;
        return Task.CompletedTask;
    }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _discountedTurn = -1;
    }

    private void RefreshDiscount(CardPlay? excludedPlay = null)
    {
        if (Owner.PlayerCombatState is not { } state || _discountedTurn == state.TurnNumber ||
            CGSAttackChain.CountAttacksPlayedThisTurn(Owner, CombatState, excludedPlay) < 2)
            return;

        EnergyCost.AddThisTurn(-1);
        _discountedTurn = state.TurnNumber;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
