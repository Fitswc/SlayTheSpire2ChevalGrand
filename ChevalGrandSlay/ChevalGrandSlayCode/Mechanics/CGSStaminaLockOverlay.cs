using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace ChevalGrandSlay.Mechanics;

[RegisterModelCapability]
public sealed class CGSStaminaLockOverlay : CardCapability, ICardOverlayContributor
{
    // 灰度和锁Overlay由 Godot 场景提供，仅在卡牌处于锁定状态时返回。
    private static readonly CardOverlayContribution LockOverlay = CardOverlayContribution.FromScenePath(
        $"{Entry.ModId}.stamina-lock", $"{Entry.ResPath}/scenes/cards/CGSStaminaLock.tscn", 0, true);

    private static readonly CardOverlayContribution RecoveryOverlay = CardOverlayContribution.FromScenePath(
        $"{Entry.ModId}.stamina-recovery", $"{Entry.ResPath}/scenes/cards/CGSStaminaRecovery.tscn", 0, true);

    public IEnumerable<CardOverlayContribution> GetCardOverlays(CardOverlayContext context)
    {
        var card = context.Card;
        if (CGSStamina.IsLocked(card))
            return [LockOverlay];

        // 淡蓝色由已生效的恢复能力标记。
        if (card.Pile?.Type == Entry.CGSFatePile &&
            card.Owner.Creature.Powers.OfType<ICGSStaminaRecoverySource>()
                .Any(source => source.IsRecoveringStamina(card)))
            return [RecoveryOverlay];

        return [];
    }
}
