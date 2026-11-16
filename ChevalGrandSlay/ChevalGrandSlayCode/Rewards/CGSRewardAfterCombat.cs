using System.Text.Json;
using System.Text.Json.Serialization;
using ChevalGrandSlay;
using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib;
using STS2RitsuLib.Combat.Rewards;
using STS2RitsuLib.Patching.Models;

namespace ChevalGrandSlayCode.Rewards;

internal sealed record CGSStaminaRewardPayload(string[]? CardIds);

[JsonSerializable(typeof(CGSStaminaRewardPayload))]
internal sealed partial class CGSRewardJsonContext : JsonSerializerContext;

public sealed class CGSRewardAfterCombat : ModCustomReward
{
    private const int ChoiceCount = 3;
    private static RewardType _rewardType;
    private readonly CardModel[] _choices;
    private bool _skipped;

    public CGSRewardAfterCombat(Player player, string[]? savedCardIds = null) : base(player)
    {
        var pool = ModelDb.CardPool<CGSCardPool>();
        var candidates = savedCardIds is null ? RollChoices(pool) : RestoreChoices(pool, savedCardIds);
        _choices = candidates.Select(card => Player.RunState.CreateCard(card, Player)).ToArray();
    }

    public override RewardType ModRewardType => _rewardType;

    protected override string RewardIconPath => $"{Entry.ResPath}/images/ui/reward_screen/reward_icon_cheval.png";
    protected override string DescriptionLocKey => "CGSREWARDAFTERCOMBAT_REWARD_TOKEN";

    internal static void Register()
    {
        _rewardType = ModRewardRegistry.For(Entry.ModId)
            .RegisterOwned<CGSStaminaRewardPayload>(
                "token",
                CGSRewardJsonContext.Default.CGSStaminaRewardPayload,
                (_, player, payload) => new CGSRewardAfterCombat(player, payload?.CardIds))
            .RewardType;
        CGSRestoreStaminaReward.Register();
        RitsuLibFramework.SubscribeLifecycle<CombatVictoryEvent>(AddRoomRewards, replayCurrentState: false);
    }

    private static void AddRoomRewards(CombatVictoryEvent victory)
    {
        foreach (var player in victory.RunState.Players)
        {
            if (player.Character is not CGSCharacter || player.Creature.IsDead)
                continue;

            victory.Room.ExtraRewards.TryGetValue(player, out var existing);
            if (existing?.SelectMany(reward => reward is LinkedRewardSet set ? set.Rewards : [reward])
                    .Any(reward => reward is CGSRewardAfterCombat or CGSRestoreStaminaReward) == true)
                continue;

            var reward = new CGSRewardAfterCombat(player);
            if (CGSRestoreStaminaReward.CanOffer(player))
            {
                var restore = new CGSRestoreStaminaReward(player);
                victory.Room.AddExtraReward(player, reward._choices.Length == 0
                    ? restore
                    : LinkedRewardSets.Create([reward, restore], player, LinkedRewardSelectionMode.ChooseOne));
            }
            else if (reward._choices.Length > 0)
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
        var candidates = pool
            .GetUnlockedCards(Player.UnlockState, run.CardMultiplayerConstraint)
            .Where(card => card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare)
            .Where(card => card.IsAllowedInRun(run) &&
                card.CanonicalKeywords.Contains(CGSKeywords.Stamina) &&
                CGSStamina.HasStamina(card))
            .DistinctBy(card => card.Id)
            .OrderBy(card => card.Id)
            .ToLookup(card => card.Rarity);

        var buckets = new[]
        {
            (Cards: candidates[CardRarity.Common].ToList(), Weight: 50f),
            (Cards: candidates[CardRarity.Uncommon].ToList(), Weight: 40f),
            (Cards: candidates[CardRarity.Rare].ToList(), Weight: 10f)
        }.Where(bucket => bucket.Cards.Count > 0).ToList();

        // Each rarity has its own weight, independent of how many cards it contains.
        for (int i = 0; i < ChoiceCount && buckets.Count > 0; i++)
        {
            var bucket = Rng.WeightedNextItem(rng.NextFloat(), buckets, item => item.Weight, buckets[^1]);
            int index = rng.NextInt(bucket.Cards.Count);
            var card = bucket.Cards[index];
            bucket.Cards.RemoveAt(index);
            if (bucket.Cards.Count == 0)
                buckets.Remove(bucket);

            yield return card;
        }
    }

    public override void MarkContentAsSeen()
    {
        // The native selection screen marks each displayed card as seen when it opens.
    }

    public override void OnSkipped() => _skipped = true;

    protected override async Task<bool> OnSelect()
    {
        if (_skipped || SuccessfullySelected || Player.Character is not CGSCharacter || _choices.Length == 0)
            return false;

        var selected = await CardSelectCmd.FromChooseACardScreen(
            new BlockingPlayerChoiceContext(), _choices, Player, canSkip: true);
        if (selected is null)
            return false;

        return (await CardPileCmd.Add(selected, PileType.Deck)).success;
    }

    internal sealed class RestoredRoomRewardsPatch : IPatchMethod
    {
        public static string PatchId => "restore_room_rewards";

        public static ModPatchTarget[] GetTargets() =>
            [new(typeof(CombatRoom), nameof(CombatRoom.FromSerializable))];

        private static void Postfix(CombatRoom __result)
        {
            // Older saves can already contain rewards generated before the eligibility checks.
            foreach (var (player, rewards) in __result.ExtraRewards)
            {
                bool canAdd = player.Character is CGSCharacter;
                bool canRestore = CGSRestoreStaminaReward.CanOffer(player);
                for (int i = rewards.Count - 1; i >= 0; i--)
                {
                    var group = rewards[i] as LinkedRewardSet;
                    var children = group?.Rewards ?? [rewards[i]];
                    var eligible = children.Where(reward => reward switch
                    {
                        CGSRewardAfterCombat => canAdd,
                        CGSRestoreStaminaReward => canRestore,
                        _ => true
                    }).ToArray();

                    if (eligible.Length == children.Count)
                        continue;

                    if (eligible.Length == 0)
                        rewards.RemoveAt(i);
                    else if (eligible.Length == 1)
                    {
                        eligible[0].ParentRewardSet = null;
                        rewards[i] = eligible[0];
                    }
                    else if (group is not null)
                    {
                        foreach (var removed in children.Except(eligible))
                            group.RemoveReward(removed);
                    }
                }
            }
        }
    }
}
