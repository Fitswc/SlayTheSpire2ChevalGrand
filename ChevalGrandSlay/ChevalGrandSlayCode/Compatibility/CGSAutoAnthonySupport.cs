using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using MegaCrit.Sts2.Core.Modding;

[assembly: InternalsVisibleTo("ChevalGrandSlay.AutoAnthony.Bridge")]

namespace ChevalGrandSlay.Compatibility;

internal interface ICGSGeneratedCard;

internal static class CGSAutoAnthonySupport
{
    private const string ModId = "AutoAnthony";
    private const string BridgeRelativePath = "Compat/ChevalGrandSlay.AutoAnthony.Bridge.dll";
    private const string BridgeEntry = "ChevalGrandSlay.Compatibility.AutoAnthony.ChevalAutoAnthonyIntegration";

    internal static bool IsAvailable { get; private set; }

    internal static void Register()
    {
        var alreadyLoaded = ModManager.GetLoadedMods().FirstOrDefault(mod => mod.manifest?.id == ModId);
        if (alreadyLoaded is not null)
            OnModDetected(alreadyLoaded);
        else
            ModManager.OnModDetected += OnModDetected;
    }

    private static void OnModDetected(Mod mod)
    {
        if (mod.manifest?.id != ModId)
            return;

        ModManager.OnModDetected -= OnModDetected;
        if (!ModManager.GetLoadedMods().Contains(mod) ||
            !mod.assemblies.Any(assembly => assembly.GetName().Name == ModId))
            return;

        try
        {
            var mainAssembly = typeof(Entry).Assembly;
            var integration = mainAssembly.GetType(BridgeEntry) ?? LoadBridgeEntry(mainAssembly);
            if (integration is null)
                return;

            var register = integration.GetMethod("Register", BindingFlags.Public | BindingFlags.Static)
                    ?.CreateDelegate<Action>()
                ?? throw new MissingMethodException(BridgeEntry, "Register");
            register();
            if (integration.Assembly != mainAssembly)
                ModManager.AssociateAssemblyWithMod(Entry.ModId, integration.Assembly);

            IsAvailable = true;
        }
        catch (Exception error)
        {
            Entry.Logger.Warn($"[AutoAnthony] Compatibility unavailable: {error.GetBaseException().Message}");
        }
    }

    private static Type? LoadBridgeEntry(Assembly mainAssembly)
    {
        string? location = Path.GetDirectoryName(mainAssembly.Location);
        if (location is null)
            return null;

        string bridgePath = Path.Combine(location, BridgeRelativePath);
        if (!File.Exists(bridgePath))
        {
            Entry.Logger.Warn($"[AutoAnthony] Compatibility module is missing: {bridgePath}");
            return null;
        }

        var loadContext = AssemblyLoadContext.GetLoadContext(mainAssembly) ?? AssemblyLoadContext.Default;
        return loadContext.LoadFromAssemblyPath(bridgePath).GetType(BridgeEntry, throwOnError: true);
    }
}
