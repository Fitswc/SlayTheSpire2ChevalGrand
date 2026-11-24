using global::AutoAnthony;
using ChaosCardGenerator;
using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Configs;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.RunData;

namespace ChevalGrandSlay.Compatibility.AutoAnthony;

public static class ChevalAutoAnthonyIntegration
{
    private static RunSavedData<ChevalDefinitionPayload> _pool = null!;

    public static void Register()
    {
        CheckVersion(typeof(ComponentApi), 3);
        CheckVersion(typeof(ComponentPackageApi), 3);
        CheckVersion(typeof(ComponentRuntimeApi), 1);
        CheckVersion(typeof(ComponentPresentationApi), 2);
        CheckVersion(typeof(ExternalComponentCharacterApi), 4);
        CheckVersion(typeof(ComponentRunSettingsApi), 2);
        CheckVersion(typeof(NativeCardDecompositionApi), 1);

        ChevalComponents.Register();
        ExternalComponentCharacterApi.Register(new ExternalComponentCharacterRegistration(
            ChevalComponents.ProfileId, ChevalComponents.Archetype, "ChevalGrandSlay"));
        ExternalComponentCharacterApi.RegisterRuntime(new ExternalComponentCharacterRuntimeRegistration(
            ChevalComponents.ProfileId, ChevalChaosSlots.Layout.Count, ChevalChaosSlots.TypeAt,
            () => ChevalRun.IsActive, () => ModelDb.CardPool<CGSCardPool>()));

        _pool = RitsuLibFramework.GetRunSavedDataStore(Entry.ModId).Register<ChevalDefinitionPayload>("autoanthony-pool");
        ChevalHistory.Register();
        RitsuLibFramework.SubscribeLifecycle<RunSavedDataPreparingEvent>(PrepareRun, replayCurrentState: false);
        RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(RestoreRun, replayCurrentState: false);
        RitsuLibFramework.SubscribeLifecycle<RunSavingEvent>(_ => CaptureRunData(), replayCurrentState: false);
        RitsuLibFramework.SubscribeLifecycle<MainMenuReadyEvent>(_ => ChevalRun.Clear(), replayCurrentState: false);
    }

    private static void PrepareRun(RunSavedDataPreparingEvent preparing)
    {
        var run = preparing.RunState;
        ChevalRun.Clear();
        if (preparing.IsMultiplayer || !CGSMainConfigPage.IsAutoAnthonyEnabled ||
            run.Players is not [ { Character: CGSCharacter } ])
            return;

        if (ChevalRun.Prepare(run.Rng.StringSeed, ComponentRunSettingsApi.Local) is { } payload)
        {
            ChevalRun.Install(payload);
            _pool.Set(run, new ChevalDefinitionPayload { Data = payload });
        }
    }

    private static void RestoreRun(RunLoadedEvent loaded)
    {
        ChevalRun.Clear();
        var run = loaded.RunState;
        if (loaded.IsMultiplayer || run.Players is not [ { Character: CGSCharacter } ])
            return;

        try
        {
            var marker = run.Modifiers.OfType<ChevalAutoAnthonyRunSnapshot>().LastOrDefault();
            bool legacy = !_pool.TryGet(run, out var saved) || string.IsNullOrEmpty(saved?.Data);
            var payload = legacy ? marker?.VerifiedPayload : saved!.Data;
            if (string.IsNullOrEmpty(payload))
                return;

            ChevalRun.Install(payload);
            _pool.Set(run, new ChevalDefinitionPayload { Data = payload });
            ChevalHistory.CaptureDecks(run, rebuild: legacy);
            if (marker is not null && run.Modifiers is List<ModifierModel> modifiers)
                modifiers.Remove(marker);
        }
        catch (Exception error)
        {
            Entry.Logger.Warn($"[AutoAnthony] Could not restore the Cheval pool: {error.GetBaseException().Message}");
        }
    }

    private static void CaptureRunData()
    {
        if (RunManager.Instance.State is not { } run || ChevalRun.GetSavePayload() is not { } payload)
            return;

        _pool.Set(run, new ChevalDefinitionPayload { Data = payload });
        ChevalHistory.CaptureDecks(run);
    }

    private static void CheckVersion(Type api, int expected)
    {
        // Checking the inlined constant would only verify the version used to compile this assembly.
        var version = api.GetField("ApiVersion")?.GetRawConstantValue();
        if (version is not int value || value != expected)
            throw new NotSupportedException($"AutoAnthony {api.Name} v{expected} is required (found {version}).");
    }
}

internal sealed class ChevalDefinitionPayload
{
    public string Data { get; set; } = string.Empty;
}
