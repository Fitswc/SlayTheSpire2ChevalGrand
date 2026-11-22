using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using MegaCrit.Sts2.Core.Modding;

[assembly: InternalsVisibleTo("ChevalGrandSlay.AutoAnthony")]

namespace ChevalGrandSlay.Compatibility;

internal interface ICGSGeneratedCard;

internal static class CGSAutoAnthonySupport
{
    private const string ModId = "AutoAnthony";
    private const string BridgeFile = "ChevalGrandSlay.AutoAnthony.dll";
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
        if (!ModManager.GetLoadedMods().Contains(mod))
            return;

        var mainAssembly = typeof(Entry).Assembly;
        var location = Path.GetDirectoryName(mainAssembly.Location);
        if (location is null)
            return;

        var bridgePath = Path.Combine(location, BridgeFile);
        if (!File.Exists(bridgePath))
        {
            Entry.Logger.Warn("[AutoAnthony] Compatibility module is missing.");
            return;
        }

        try
        {
            var loadContext = AssemblyLoadContext.GetLoadContext(mainAssembly) ?? AssemblyLoadContext.Default;
            var bridge = loadContext.LoadFromAssemblyPath(bridgePath);
            var register = bridge.GetType(BridgeEntry, throwOnError: true)!
                    .GetMethod("Register", BindingFlags.Public | BindingFlags.Static)?.CreateDelegate<Action>()
                ?? throw new MissingMethodException(BridgeEntry, "Register");
            register();
            ModManager.AssociateAssemblyWithMod(Entry.ModId, bridge);
            IsAvailable = true;
        }
        catch (Exception error)
        {
            Entry.Logger.Warn($"[AutoAnthony] Compatibility unavailable: {error.GetBaseException().Message}");
        }
    }
}
