using MegaCrit.Sts2.Core.Models;

namespace ChevalGrandSlay.Mechanics;

// 持续恢复效果用实际结算的目标规则标记正在恢复体力的牌。
internal interface ICGSStaminaRecoverySource
{
    bool IsRecoveringStamina(CardModel card);
}
