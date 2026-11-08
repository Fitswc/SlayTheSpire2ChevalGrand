using ChevalGrandSlay.Utilities;
using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卡牌：迟来的疾驰
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSLongSprint : ModCardTemplate
{
    // 基础耗能。
    private const int BaseEnergyCost = 2;

    // 卡牌类型。
    private const CardType CardKind = CardType.Attack;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Uncommon;

    // 目标类型（AnyEnemy 表示任意敌人）。
    private const TargetType CardTarget = TargetType.AnyEnemy;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CGSKeywords.Stamina,
        CardKeyword.Retain
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Uses", 3m),
        new DynamicVar("MaxUses", 3m),
        new DamageVar(16m, ValueProp.Move),
        new DynamicVar("RetainedDamage", 8m)
    ];

    public CGSLongSprint() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);

        bool retainedLastTurn = _lastRetainedTurn == Owner.PlayerCombatState!.TurnNumber - 1;

        decimal damage = DynamicVars.Damage.BaseValue;

        if (retainedLastTurn)
        {
            damage += DynamicVars["RetainedDamage"].BaseValue;
        }

        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(damage)
            .FromCard(this).CGSWithVfx("vfx/vfx_attack_slash").Targeting(cardPlay.Target).Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        DynamicVars["Damage"].UpgradeValueBy(5m);
        DynamicVars["RetainedDamage"].UpgradeValueBy(2m);
    }
    private int _lastRetainedTurn = -1;

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _lastRetainedTurn = -1;
    }

    public override Task AfterFlush(PlayerChoiceContext choiceContext, Player player,
        IReadOnlyCollection<CardModel> flushedCards, IReadOnlyCollection<CardModel> retainedCards)
    {
        // 只记实际保留的回合，不累计伤害；复制出来的牌不继承保留记录。
        if (player == Owner && retainedCards.Contains(this))
            _lastRetainedTurn = player.PlayerCombatState!.TurnNumber;
        return Task.CompletedTask;
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        CGSConsumeUse.GetResultPileTypeForCardPlay(this, base.GetResultPileTypeForCardPlay());
}
