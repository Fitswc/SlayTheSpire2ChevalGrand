using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Enchantments;

[RegisterEnchantment]
public sealed class CGSReserveStepEnchantment : ModEnchantmentTemplate
{
    public override bool ShowAmount => false;
    public override bool HasExtraCardText => false;
//    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
//        [HoverTipFactory.FromKeyword(CGSKeywords.Stamina)];

    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/enchantments/{GetType().Name}.png");

    public override bool CanEnchant(CardModel card) =>
        card.DynamicVars.TryGetValue("Uses", out var uses) && uses.IntValue > 0 &&
        (card.Enchantment is CGSReserveStepEnchantment || base.CanEnchant(card));

    protected override void OnEnchant()
    {
        Status = EnchantmentStatus.Normal;
    }

    // 只保护附魔所在的牌，本场战斗中每次打出都不消耗体力。
    public bool PreservesStamina(CardModel card) =>
        Card == card && Status == EnchantmentStatus.Normal;

    public override Task AfterCombatEnd(CombatRoom room)
    {
        RemoveFromCard();
        return Task.CompletedTask;
    }

    private void RemoveFromCard()
    {
        if (Card is { } card && card.Enchantment == this)
            CardCmd.ClearEnchantment(card);
    }
}
