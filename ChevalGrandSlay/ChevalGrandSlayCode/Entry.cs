using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Audio;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Interop;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace ChevalGrandSlay;


//TODO:修改card.json的决意图标和文本

[ModInitializer(nameof(Initialize))]
public partial class Entry
{
    // ModId 需要和 ChevalGrandSlay.json 里的 id 保持一致。
    // res://ChevalGrandSlay/... 里的 ChevalGrandSlay 是 PCK 资源目录，不是 C# namespace。
    public const string ModId = "ChevalGrandSlay";
    public const string ResPath = $"res://{ModId}";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static PileType CGSFatePile { get; private set; }

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        // 以下示例默认已经在 Entry.Initialize() 中调用了
        // RitsuLibFramework.EnsureGodotScriptsRegistered(...) 和
        // ModTypeDiscoveryHub.RegisterModAssembly(...)，否则自动注册不会生效。
        //
        // Godot C# 脚本注册只负责让 pck 中的脚本类型能被 Godot 找到。
        // 这一步和 RitsuLib 的内容自动注册不是同一件事，两个都需要保留。
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);

        // 自动注册扫描会读取当前程序集里的 RegisterCard/RegisterRelic 等 attribute。
        // 新增内容类后，只要 attribute 写对，通常不需要在入口里手动逐个注册。
        // 卡牌构造时需要有效的资源标识。
        CGSDetermination.Register();
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        FmodStudioDeferredBankRegistration.RegisterBank($"{ResPath}/audios/ChevalGrandSlaySFX.bank");
        FmodStudioDeferredBankRegistration.RegisterStudioGuidMappings($"{ResPath}/audios/GUIDs.txt");

        //Patch
        CharacterSelectAudio.Install();

        //Fate CardPile Register
        var registry = ModCardPileRegistry.For(ModId);
        CGSFatePile = registry.RegisterOwned("fate", new ModCardPileSpec
        {
            // Scope 决定了牌堆的生命周期，
            // CombatOnly：每次战斗创建，战斗结束时销毁
            // RunPersistent：同一局游戏内可跨战斗保留（仅存于内存，需自行写入存档）
            Scope = ModCardPileScope.CombatOnly,
            // Style 决定了牌堆按钮放置的位置，
            // Headless：不可见
            // TopBarDeck：顶栏牌组按钮旁
            // BottomLeft：战斗UI左下（抽牌堆附近）
            // BottomRight：战斗UI右下（消耗堆附近）
            // ExtraHand：额外手牌容器
            Style = ModCardPileUiStyle.BottomLeft,
            // 锚点，见下
            Anchor = ModCardPileAnchor.Default,
            IconPath = $"{ResPath}/images/fate_pile.png",
            // 点击打开
            OnOpen = ctx => ctx.ShowDefaultPileScreen(),
            VisibleWhen = ctx => ctx.Player != null,
        }).PileType;


        Logger.Info("ChevalGrandMod initialized.");
    }
}