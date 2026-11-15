using ChevalGrandSlay;
using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;
using STS2RitsuLib.Combat.Rewards;

namespace ChevalGrandSlayCode.Rewards;

internal sealed class CGSRestoreStaminaReward(Player player) : ModCustomReward(player)
{
    private static RewardType _rewardType;
    private bool _skipped;

    public override RewardType ModRewardType => _rewardType;
    protected override string RewardIconPath => $"{Entry.ResPath}/images/ui/reward_screen/reward_icon_cheval.png";
    protected override string DescriptionLocKey => "CGS_RESTORE_STAMINA_REWARD";

    internal static bool CanOffer(Player player) =>
        player.Character is CGSCharacter && player.Deck.Cards.Any(CGSStamina.CanRestoreDeckCard);

    internal static void Register()
    {
        _rewardType = ModRewardRegistry.For(Entry.ModId)
            .RegisterOwned("restore_stamina", (_, player, _) => new CGSRestoreStaminaReward(player))
            .RewardType;
    }

    public override void MarkContentAsSeen()
    {
    }

    public override void OnSkipped() => _skipped = true;

    protected override async Task<bool> OnSelect()
    {
        if (_skipped || SuccessfullySelected || !CanOffer(Player))
            return false;

        var prompt = new LocString("gameplay_ui", "CGS_RESTORE_STAMINA_REWARD_PROMPT");
        var preferences = new CardSelectorPrefs(prompt, selectCount: 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };
        var selected = await CardSelectCmd.FromDeckGeneric(Player, preferences, CGSStamina.CanRestoreDeckCard);
        return selected.FirstOrDefault() is { } card && CGSStamina.RestoreDeckCard(card);
    }
}
