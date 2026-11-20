using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

namespace ChevalGrandSlay.Configs;

public sealed class CGSMainConfig
{
    public bool Disable { get; set; } = false;
}

public static class CGSMainConfigPage
{
    private const string DataKey = "CGSsettings";

    private static readonly ModSettingsValueBinding<CGSMainConfig, bool> CGSAutoAnthonyBinding = new(
        Entry.ModId, DataKey, SaveScope.Profile,
        static s => s.Disable,
        static (s, v) => s.Disable = v);

    public static void Register()
    {
        // 注册 DataStore
        ModDataStore.For(Entry.ModId).Register<CGSMainConfig>(
            key: DataKey, // 持久化数据ID，需要和别人防撞
            fileName: "CGSsettings.json", // 你的数据文件名
            scope: SaveScope.Profile, // Profile 表示每个存档独立，可改成 Global 表示所有存档共享
            defaultFactory: () => new CGSMainConfig(),
            autoCreateIfMissing: true);

        // 注册页面UI
        RitsuLibFramework.RegisterModSettings(Entry.ModId, page => page
            .WithTitle(ModSettingsText.Literal("ChevalGrand"))
            .WithModDisplayName(ModSettingsText.Literal("ChevalGrand Mod"))
            .WithVisibleOnHostSurfaces(
                ModSettingsHostSurface.MainMenu | ModSettingsHostSurface.RunPause)
            .AddSection("general", section => section
                .WithTitle(ModSettingsText.Literal("兼容"))
                .AddToggle("disabled", ModSettingsText.Literal("东尼算法兼容"), CGSAutoAnthonyBinding)));
    }
    
}