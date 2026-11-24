using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.HandSize;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

// 能力：我憧憬的蓝
[RegisterPower]
public sealed class CGSWideViewPower : ModPowerTemplate, IMaxHandSizeModifier, ICGSStaminaRecoverySource
{
    internal const string RecoveryCountVar = "RecoveryCount";
    internal const string RecoveryAmountVar = "RecoveryAmount";
    internal const int BaseRecoveryCount = 2;
    internal const decimal BaseRecoveryAmount = 1m;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    // 自定义图标路径。1:1即可。原版游戏大图256x256，小图64x64。
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/small/CGSWideView.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/CGSWideView.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(RecoveryCountVar, BaseRecoveryCount),
        new DynamicVar(RecoveryAmountVar, BaseRecoveryAmount)
    ];

    internal void SetRecovery(int count, decimal amount)
    {
        var recoveryCount = DynamicVars[RecoveryCountVar];
        recoveryCount.BaseValue = Math.Max(recoveryCount.IntValue, count);
        var recoveryAmount = DynamicVars[RecoveryAmountVar];
        recoveryAmount.BaseValue = Math.Max(recoveryAmount.BaseValue, amount);
    }

    // 卡面标记与回合结算共用同一份目标规则，按休整牌堆的顺序分配恢复名额。
    private IEnumerable<CardModel> GetRecoveryTargets(CardPile restPile) =>
        restPile.Cards
            .Where(CGSStamina.CanRecoverStamina)
            .Take(DynamicVars[RecoveryCountVar].IntValue);

    bool ICGSStaminaRecoverySource.IsRecoveringStamina(CardModel card) =>
        Owner.Player is { } player &&
        GetRecoveryTargets(Entry.CGSFatePile.GetPile(player)).Contains(card, ReferenceEqualityComparer.Instance) &&
        !CGSStamina.IsFull(card);

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        var restPile = Entry.CGSFatePile.GetPile(player);
        var restingCards = GetRecoveryTargets(restPile).ToArray();
        decimal amount = DynamicVars[RecoveryAmountVar].BaseValue;
        foreach (var card in restingCards)
        {
            if (card.Pile != restPile)
                continue;

            CGSStamina.Restore(card, amount);
            if (CGSStamina.IsFull(card))
            {
                // Add moves the existing card out of its source pile before inserting it.
                await CardPileCmd.Add(card, PileType.Draw);
            }
        }
    }

    public int ModifyMaxHandSize(Player player, int currentMaxHandSize) =>
        player == Owner.Player ? currentMaxHandSize + Amount : currentMaxHandSize;
}
