using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using global::AutoAnthony;
using ChaosCardGenerator;
using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Models;

namespace ChevalGrandSlay.Compatibility.AutoAnthony;

internal sealed record ChevalPoolSnapshot(
    int Schema,
    bool PreserveOriginalCards,
    IReadOnlyList<ChaosCardDefinition> Definitions);

internal static class ChevalRun
{
    internal const int Schema = 1;
    internal const int MaxPayloadSize = 128 * 1024;
    private const int MaxSnapshotSize = 4 * 1024 * 1024;

    private static readonly string[] AttackPortraits = ["CGSLongSprint", "CGSChainStep", "CGSFinalSprint"];
    private static readonly string[] SkillPortraits = ["CGSBriefRestRestart", "CGSCounterAttack", "CGSReorderNotes"];
    private static readonly string[] PowerPortraits = ["CGSSubZero", "CGSWideView", "CGSBorrowFromThePast"];
    private static string? _activePayload;
    private static ChevalPoolSnapshot? _activeSnapshot;

    private sealed record CardPayload(ChaosCardDefinition Definition, OperationLocalizedText?[] TextTemplates);

    private sealed record PoolPayload(int Schema, bool PreserveOriginalCards, CardPayload[] Cards);

    internal static bool IsActive => _activePayload is not null;

    internal static string? GetSavePayload()
    {
        if (_activeSnapshot is null)
            return null;

        var definitions = ExternalComponentCharacterApi.GetDefinitions(ChevalComponents.ProfileId);
        if (!ReferenceEquals(definitions, _activeSnapshot.Definitions))
        {
            var updated = _activeSnapshot with { Definitions = definitions };
            _activePayload = Encode(updated);
            _activeSnapshot = updated;
        }
        return _activePayload;
    }

    internal static string? Prepare(string seed, ComponentRunSettings settings)
    {
        if (!settings.Enabled || !settings.AddGeneratedCards)
            return null;

        var definitions = Generate(seed, settings);
        return Encode(new ChevalPoolSnapshot(Schema, settings.PreserveOriginalCards, definitions));
    }

    internal static IReadOnlyList<ChaosCardDefinition> Generate(string seed, ComponentRunSettings settings)
    {
        var seeds = SHA256.HashData(Encoding.UTF8.GetBytes(ChevalComponents.ProfileId + "|" + seed));
        var random = new Random(BinaryPrimitives.ReadInt32LittleEndian(seeds));
        var generators = ChevalChaosSlots.Layout.Select(slot => slot.Type).Distinct().ToDictionary(type => type,
            type => new RandomCardGenerator(ChevalComponents.RequestFor(type), random.Next(),
                suppressDerivativeReferences: true,
                balancedValues: settings.NumericBalanceOptimization, randomizeNumericValues: settings.NumericRandomMode));
        var art = new Random(BinaryPrimitives.ReadInt32LittleEndian(seeds.AsSpan(4)));
        var names = ComponentApi.Resolve(ChevalComponents.Request).NameCatalog;
        var chineseNames = names.Recipes.Select(recipe => recipe.ChineseTitle).ToHashSet(StringComparer.Ordinal);
        var englishNames = names.Recipes.Select(recipe => recipe.EnglishTitle).ToHashSet(StringComparer.Ordinal);
        var definitions = new List<ChaosCardDefinition>(ChevalChaosSlots.Layout.Count);
        foreach (var (rarity, type, _) in ChevalChaosSlots.Layout)
        {
            var card = generators[type].Generate(rarity);
            if (card.Type != type)
                throw new InvalidOperationException($"Cheval AutoAnthony generator did not fill the {rarity} {type} quota.");

            // Final Kick requires zero available energy before play, so a paid or X-cost shell would trap it in hand.
            if (ChevalComponents.HasEffect(card, ChevalComponents.FinalKick))
            {
                var upgrade = card.Upgrade;
                card = card with
                {
                    Cost = 0,
                    Upgrade = upgrade is null ? null : upgrade with
                    {
                        UpgradedCost = 0,
                        Effects = upgrade.Effects.Where(effect => effect.Kind != CardUpgradeKind.ReduceCost).ToArray()
                    }
                };
            }

            card = card with { Name = CardNameGenerator.Generate(names, card, random, chineseNames, englishNames) };
            chineseNames.Add(card.Name.Chinese);
            englishNames.Add(card.Name.English);
            var portraits = card.Type switch
            {
                GeneratedCardType.Attack => AttackPortraits,
                GeneratedCardType.Power => PowerPortraits,
                _ => SkillPortraits
            };
            string portrait = portraits[settings.RandomCardArt ? art.Next(portraits.Length) : definitions.Count % portraits.Length];
            definitions.Add(new ChaosCardDefinition(definitions.Count, card,
                $"res://ChevalGrandSlay/images/cards/{portrait}.png",
                "vfx/vfx_attack_slash",
                card.Type == GeneratedCardType.Attack ? "Attack" : "Cast",
                "res://images/atlases/power_atlas.sprites/plating_power.tres",
                "res://images/powers/plating_power.png",
                card.Operations.Select(OperationRuntimeSpecCompiler.RequireStructured).ToArray(),
                card.Upgrade?.Effects.Select(effect => effect.ValueSlotId).ToArray() ?? [],
                PortraitSourceId: portrait));
        }

        return definitions;
    }

    internal static void Install(string? payload)
    {
        if (StringComparer.Ordinal.Equals(_activePayload, payload))
            return;

        if (payload is null)
        {
            Clear();
            return;
        }

        var snapshot = Decode(payload);
        var slots = ChevalChaosSlots.GetCards();
        var pool = ModelDb.CardPool<CGSCardPool>();
        ExternalComponentCharacterApi.InstallDefinitions(ChevalComponents.ProfileId, snapshot.Definitions);
        try
        {
            pool.SetGeneratedCards(slots, snapshot.PreserveOriginalCards);
            _activePayload = payload;
            _activeSnapshot = snapshot with { Definitions = ExternalComponentCharacterApi.GetDefinitions(ChevalComponents.ProfileId) };
        }
        catch
        {
            ExternalComponentCharacterApi.ClearDefinitions(ChevalComponents.ProfileId);
            pool.ClearGeneratedCards();
            _activePayload = null;
            _activeSnapshot = null;
            throw;
        }
    }

    internal static void Clear()
    {
        if (!IsActive)
            return;

        ModelDb.CardPool<CGSCardPool>().ClearGeneratedCards();
        ExternalComponentCharacterApi.ClearDefinitions(ChevalComponents.ProfileId);
        _activePayload = null;
        _activeSnapshot = null;
    }

    internal static string Encode(ChevalPoolSnapshot snapshot)
    {
        var pool = new PoolPayload(snapshot.Schema, snapshot.PreserveOriginalCards,
            snapshot.Definitions.Select(CreateCardPayload).ToArray());
        using var bytes = new MemoryStream();
        using (var zip = new BrotliStream(bytes, CompressionLevel.Optimal, leaveOpen: true))
            JsonSerializer.Serialize(zip, pool);
        string payload = Convert.ToBase64String(bytes.ToArray());
        if (payload.Length > MaxPayloadSize)
            throw new InvalidDataException("Cheval AutoAnthony pool exceeds the snapshot size limit.");
        return payload;
    }

    internal static ChevalPoolSnapshot Decode(string payload)
    {
        if (payload.Length is 0 or > MaxPayloadSize)
            throw new InvalidDataException("Invalid Cheval AutoAnthony snapshot size.");
        using var compressed = new MemoryStream(Convert.FromBase64String(payload), writable: false);
        using var zip = new BrotliStream(compressed, CompressionMode.Decompress);
        using var bytes = new MemoryStream();
        var buffer = new byte[8192];
        for (int read; (read = zip.Read(buffer)) != 0;)
        {
            if (bytes.Length + read > MaxSnapshotSize)
                throw new InvalidDataException("Cheval AutoAnthony snapshot exceeds the decompressed size limit.");
            bytes.Write(buffer, 0, read);
        }

        bytes.Position = 0;
        var snapshot = JsonSerializer.Deserialize<PoolPayload>(bytes);
        if (snapshot?.Schema != Schema || snapshot.Cards?.Length != ChevalChaosSlots.Layout.Count)
            throw new InvalidDataException("Unsupported Cheval AutoAnthony snapshot schema or slot count.");

        var definitions = new ChaosCardDefinition[snapshot.Cards.Length];
        for (int i = 0; i < snapshot.Cards.Length; i++)
            definitions[i] = RestoreDefinition(snapshot.Cards[i], i);
        return new ChevalPoolSnapshot(snapshot.Schema, snapshot.PreserveOriginalCards, definitions);
    }

    private static CardPayload CreateCardPayload(ChaosCardDefinition definition) =>
        new(definition, definition.Card.Operations.Select(operation => operation.LocalizedText).ToArray());

    internal static string EncodeCard(ChaosCardDefinition definition) =>
        JsonSerializer.Serialize(CreateCardPayload(definition));

    internal static ChaosCardDefinition DecodeCard(string payload, int slot)
    {
        if (payload.Length is 0 or > MaxPayloadSize)
            throw new InvalidDataException("Invalid Cheval AutoAnthony card payload size.");
        var entry = JsonSerializer.Deserialize<CardPayload>(payload)
            ?? throw new InvalidDataException("Missing Cheval AutoAnthony card payload.");
        return RestoreDefinition(entry, slot);
    }

    private static ChaosCardDefinition RestoreDefinition(CardPayload entry, int i)
    {
        var definition = entry.Definition;
        var card = definition.Card;
        var upgrade = card.Upgrade;
        var slot = ChevalChaosSlots.Layout[i];
        int operationCount = card.Operations.Count;
        if (definition.Slot != i || (card.Rarity, card.Type) != (slot.Rarity, slot.Type)
            || card.Character != ChevalComponents.Archetype || operationCount == 0
            || definition.RuntimeSpecs?.Count != operationCount || entry.TextTemplates?.Length != operationCount
            || definition.UpgradeValueSlots?.Count != (upgrade?.Effects.Count ?? 0))
            throw new InvalidDataException($"Invalid Cheval AutoAnthony definition for slot {i}.");

        var operations = new GeneratorOperation[operationCount];
        for (int opIndex = 0; opIndex < operationCount; opIndex++)
        {
            var spec = definition.RuntimeSpecs[opIndex];
            var template = entry.TextTemplates[opIndex];
            spec.Validate();
            template?.Validate(spec);
            if (spec.Opcode == ChevalComponents.RuntimeOpcode && !ComponentRuntimeApi.HasRoute(spec.Opcode, spec.Variant))
                throw new InvalidDataException($"Unsupported Cheval AutoAnthony effect {spec.Variant}.");
            operations[opIndex] = card.Operations[opIndex] with { RuntimeSpec = spec, LocalizedText = template };
        }

        if (upgrade is not null)
            upgrade = upgrade with
            {
                Effects = upgrade.Effects.Select((effect, index) => effect with
                {
                    ValueSlotId = definition.UpgradeValueSlots[index]
                }).ToArray()
            };

        return definition with { Card = card with { Operations = operations, Upgrade = upgrade } };
    }
}
