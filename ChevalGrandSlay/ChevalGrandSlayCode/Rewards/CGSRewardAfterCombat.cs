using System.Text.Json;
using System.Text.Json.Serialization;
using ChevalGrandSlay;
using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using STS2RitsuLib;
using STS2RitsuLib.Combat.Rewards;

namespace ChevalGrandSlayCode.Rewards;

internal sealed record CGSStaminaRewardPayload(string[]? CardIds);

[JsonSerializable(typeof(CGSStaminaRewardPayload))]
internal sealed partial class CGSRewardJsonContext : JsonSerializerContext;

public sealed class CGSRewardAfterCombat : ModCustomReward
{
    private const int ChoiceCount = 3;
    private static RewardType _rewardType;
    private readonly CardModel[] _choices;

    public CGSRewardAfterCombat(Player player, string[]? savedCardIds = null) : base(player)
    {
        var pool = ModelDb.CardPool<CGSCardPool>();
        var candidates = savedCardIds is null ? RollChoices(pool) : RestoreChoices(pool, savedCardIds);
        _choices = candidates.Select(card => Player.RunState.CreateCard(card, Player)).ToArray();
    }

    public override RewardType ModRewardType => _rewardType;

    protected override string RewardIconPath => "res://images/ui/reward_screen/reward_icon_card.png";
    protected override string DescriptionLocKey => "CGSREWARDAFTERCOMBAT_REWARD_TOKEN";

    internal static void Register()
    {
        _rewardType = ModRewardRegistry.For(Entry.ModId)
            .RegisterOwned<CGSStaminaRewardPayload>(
                "token",
                CGSRewardJsonContext.Default.CGSStaminaRewardPayload,
                (_, player, payload) => new CGSRewardAfterCombat(player, payload?.CardIds))
            .RewardType;
        RitsuLibFramework.SubscribeLifecycle<CombatVictoryEvent>(AddRoomRewards, replayCurrentState: false);
    }

    private static void AddRoomRewards(CombatVictoryEvent victory)
    {
        foreach (var player in victory.RunState.Players)
        {
            if (player.Character is not CGSCharacter || player.Creature.IsDead)
                continue;

            victory.Room.ExtraRewards.TryGetValue(player, out var existing);
            if (existing?.Any(reward => reward is CGSRewardAfterCombat) == true)
                continue;

            var reward = new CGSRewardAfterCombat(player);
            if (reward._choices.Length > 0)
                victory.Room.AddExtraReward(player, reward);
        }
    }

    public override string ToModRewardJson()
    {
        var cardIds = _choices.Select(card => card.Id.ToString()).ToArray();
        return JsonSerializer.Serialize(new CGSStaminaRewardPayload(cardIds),
            CGSRewardJsonContext.Default.CGSStaminaRewardPayload);
    }

    private static IEnumerable<CardModel> RestoreChoices(CGSCardPool pool, string[] cardIds)
    {
        var cardsById = pool.AllCards.ToLookup(card => card.Id.ToString(), StringComparer.Ordinal);
        return cardIds
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .Select(id => cardsById[id].FirstOrDefault())
            .OfType<CardModel>()
            .Take(ChoiceCount);
    }

    private IEnumerable<CardModel> RollChoices(CGSCardPool pool)
    {
        var run = Player.RunState;
        // Also makes the fallback for older saves consistent across clients.
        var rng = new Rng(run.Rng.Seed, $"{Entry.ModId}.StaminaReward.{run.TotalFloor}.{run.GetPlayerSlotIndex(Player)}");
        return pool
            .GetUnlockedCards(Player.UnlockState, run.CardMultiplayerConstraint)
            .Where(card => card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare)
            .Where(card => card.IsAllowedInRun(run) &&
                card.CanonicalKeywords.Contains(CGSKeywords.Stamina) &&
                CGSStamina.HasStamina(card))
            .DistinctBy(card => card.Id)
            .OrderBy(card => card.Id)
            .TakeRandom(ChoiceCount, rng);
    }

    public override void MarkContentAsSeen()
    {
        // The native selection screen marks each displayed card as seen when it opens.
    }

    protected override async Task<bool> OnSelect()
    {
        if (_choices.Length == 0)
            return false;

        var selected = await CardSelectCmd.FromChooseACardScreen(
            new BlockingPlayerChoiceContext(), _choices, Player, canSkip: true);
        if (selected is null)
            return false;

        return (await CardPileCmd.Add(selected, PileType.Deck)).success;
    }
}
