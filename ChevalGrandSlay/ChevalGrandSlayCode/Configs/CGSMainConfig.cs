using ChevalGrandSlay.Compatibility;
using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

namespace ChevalGrandSlay.Configs;

public sealed class CGSMainConfig
{
    // Keep the original JSON field name. The existing toggle stores true when it is on.
    public bool Disable { get; set; } = false;
}

public static class CGSMainConfigPage
{
    private const string DataKey = "CGSsettings";

    private static readonly ModSettingsValueBinding<CGSMainConfig, bool> CGSAutoAnthonyBinding = new(
        Entry.ModId, DataKey, SaveScope.Profile,
        static s => CGSAutoAnthonySupport.IsAvailable && s.Disable,
        static (s, v) =>
        {
            if (CGSAutoAnthonySupport.IsAvailable)
                s.Disable = v;
        });

    internal static bool IsAutoAnthonyEnabled => CGSAutoAnthonyBinding.Read();

    public static void Register()
    {
        // 注册 DataStore
        ModDataStore.For(Entry.ModId).Register<CGSMainConfig>(
            key: DataKey, // 持久化数据ID，需要和别人防撞
            fileName: "CGSsettings.json", // 你的数据文件名
            scope: SaveScope.Profile, // Profile 表示每个存档独立，可改成 Global 表示所有存档共享
            defaultFactory: () => new CGSMainConfig(),
            autoCreateIfMissing: true);

        var localization = RitsuLibFramework.CreateModLocalization(
            Entry.ModId, "settings", resourceFolders: [$"{Entry.ModId}.Settings"],
            resourceAssembly: typeof(CGSMainConfigPage).Assembly);

        // 注册页面UI
        RitsuLibFramework.RegisterModSettings(Entry.ModId, page => page
            .WithTitle(Localize("cgs.page.title"))
            .WithModDisplayName(Localize("cgs.page.modName"))
            .WithVisibleOnHostSurfaces(
                ModSettingsHostSurface.MainMenu | ModSettingsHostSurface.RunPause)
            .AddSection("general", section => section
                .WithTitle(Localize("cgs.compatibility.title"))
                .WithEnabledWhen(static () => CGSAutoAnthonySupport.IsAvailable)
                .AddToggle("disabled",
                    Localize("cgs.autoAnthony.label"),
                    CGSAutoAnthonyBinding,
                    Localize("cgs.autoAnthony.description"))));
        return;

        ModSettingsText Localize(string key) => ModSettingsText.I18N(localization, key, key);
    }
}
