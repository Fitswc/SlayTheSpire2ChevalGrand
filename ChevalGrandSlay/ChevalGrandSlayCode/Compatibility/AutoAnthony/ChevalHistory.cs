using global::AutoAnthony;
using ChaosCardGenerator;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Models.Capabilities;

namespace ChevalGrandSlay.Compatibility.AutoAnthony;

internal static class ChevalHistory
{
    private static ModelSavedData<ChevalChaosCard, ChevalDefinitionPayload> _cardDefinition = null!;

    internal static void Register()
    {
        _cardDefinition = ModelSavedDataStore.For(Entry.ModId)
            .Register<ChevalChaosCard, ChevalDefinitionPayload>("autoanthony-card");

        RitsuLibFramework.GetModelCloneRegistry(Entry.ModId)
            .Register<ChevalChaosCard>("autoanthony-card-definition", (prototype, clone) =>
            {
                if (prototype.IsCanonical && ChevalRun.IsActive)
                    Capture(clone);
            });
    }

    internal static void CaptureDecks(RunState run, bool rebuild = false)
    {
        foreach (var card in run.Players.SelectMany(player => player.Deck.Cards).OfType<ChevalChaosCard>())
        {
            Capture(card);
            if (rebuild)
                card.RefreshRestoredState();
        }
    }

    private static void Capture(ChevalChaosCard card) =>
        _cardDefinition.Set(card, new ChevalDefinitionPayload { Data = ChevalRun.EncodeCard(card.Definition) });

    internal static string? Read(ChevalChaosCard card) =>
        _cardDefinition is not null && _cardDefinition.TryGet(card, out var value) ? value?.Data : null;

    private static readonly ChaosCardDefinition[] Unavailable = ChevalChaosSlots.Layout.Select((slot, index) =>
        new ChaosCardDefinition(index, new GeneratedCard(
                0, slot.Type, TargetMode.Other, slot.Rarity, "这张随机卡缺少已保存的效果定义。", [], [],
                new GeneratedCardName("随机卡", "Generated Card", []),
                EnglishDescription: "This generated card is missing its saved definition.",
                Character: ChevalComponents.Archetype),
            "res://ChevalGrandSlay/images/cards/CGSStrike.png", "vfx/vfx_attack_slash", "Cast",
            "res://images/atlases/power_atlas.sprites/plating_power.tres",
            "res://images/powers/plating_power.png", [], [])).ToArray();

    internal static ChaosCardDefinition Placeholder(int slot) => Unavailable[slot];
}
