using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 长线冲刺
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSLongSprint : ModCardTemplate
{
    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(16m, ValueProp.Move),
        new DynamicVar("RetainedDamage", 8m)
    ];

    public CGSLongSprint() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true)
    {
        this.SecondaryResourceUses().Require("determination", CGSDetermination.CGSDeterminationId, 10);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        bool retainedLastTurn = _lastRetainedTurn == Owner.PlayerCombatState!.TurnNumber - 1;
        decimal damage = DynamicVars.Damage.BaseValue;
        if (retainedLastTurn)
            damage += DynamicVars["RetainedDamage"].BaseValue;
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(damage)
            .FromCard(this).Targeting(cardPlay.Target).Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
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
}
