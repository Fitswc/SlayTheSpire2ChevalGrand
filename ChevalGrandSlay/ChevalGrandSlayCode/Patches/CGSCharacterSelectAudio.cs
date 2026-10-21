using MegaCrit.Sts2.Core.Combat;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using STS2RitsuLib.Audio;

namespace ChevalGrandSlay;

[HarmonyPatch(typeof(SfxCmd), nameof(SfxCmd.Play), new[] { typeof(string), typeof(float) })]
internal static class CGSCharacterSelectAudio
{
    private const string Intro = "event:/ChevalGrandSlaySFX/SFX/CGSSIntroduce";
    private const string Transition = "event:/ChevalGrandSlaySFX/SFX/CGSSTransition";

    private static AudioEventHandle? _introHandle;

    [HarmonyPrepare]
    private static void Prepare(MethodBase? original)
    {
        // original 为空时是整个补丁的回调，避免逐个目标重复输出。
        if (original == null)
            Entry.Logger.Info($"[Patch] {nameof(CGSCharacterSelectAudio)} Start Patching");
    }

    [HarmonyCleanup]
    private static void Cleanup(MethodBase? original, Exception? exception)
    {
        if (original != null)
            return;

        // 游戏日志接口同时输出到控制台和日志；不吞掉注入异常。
        if (exception == null)
            Entry.Logger.Info($"[Patch] {nameof(CGSCharacterSelectAudio)} Patch Successful");
        else
            Entry.Logger.Error($"[Patch] {nameof(CGSCharacterSelectAudio)} Patch Failed: {exception}");
    }

    [HarmonyPrefix]
    public static bool Prefix([HarmonyArgument(0)] string sfx, [HarmonyArgument(1)] float volume)
    {
        if (sfx != Intro && sfx != Transition)
            return true;

        // 新的介绍或转场开始前，先停止上一次介绍；转场也覆盖多人同步出发。
        StopIntro();

        if (sfx == Transition)
            return true;

        // 保留 SfxCmd 原有的播放条件。
        if (NonInteractiveMode.IsActive || CombatManager.Instance.IsEnding)
            return false;

        _introHandle = FmodStudioEventInstances.TryCreateHandle(
            AudioSource.Event(Intro), new AudioPlaybackOptions());
        if (_introHandle == null)
        {
            Entry.Logger.Info("The character introduction audio instance cannot be created.");
            return false;
        }

        _introHandle.TrySetVolume(volume);

        if (!_introHandle.TryPlay())
            StopIntro();

        // 自我介绍由本补丁播放，跳过原方法，避免重复播放。
        return false;
    }

    internal static void StopIntro()
    {
        var handle = _introHandle;
        _introHandle = null;

        handle?.TryStop(allowFadeOut: false);
        handle?.TryRelease();
    }
}

// 切换角色、出发或关闭子菜单时，停止尚未播完的自我介绍。
[HarmonyPatch]
internal static class CGSCharacterSelectStopIntroPatch
{
    [HarmonyPrepare]
    private static void Prepare(MethodBase? original)
    {
        // original 为空时是整个补丁的回调，避免逐个目标重复输出。
        if (original == null)
            Entry.Logger.Info($"[Patch] {nameof(CGSCharacterSelectStopIntroPatch)} Start Patching");
    }

    [HarmonyCleanup]
    private static void Cleanup(MethodBase? original, Exception? exception)
    {
        if (original != null)
            return;

        // 游戏日志接口同时输出到控制台和日志；不吞掉注入异常。
        if (exception == null)
            Entry.Logger.Info($"[Patch] {nameof(CGSCharacterSelectStopIntroPatch)} Patch Successful");
        else
            Entry.Logger.Error($"[Patch] {nameof(CGSCharacterSelectStopIntroPatch)} Patch Failed: {exception}");
    }

    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods() =>
    [
        AccessTools.Method(typeof(NCharacterSelectScreen), "SelectCharacter"),
        AccessTools.Method(typeof(NCharacterSelectScreen), "OnLocalCharacterChangedForRandom"),
        AccessTools.Method(typeof(NCharacterSelectScreen), "OnEmbarkPressed"),
        AccessTools.Method(typeof(NCharacterSelectScreen), "OnSubmenuClosed")
    ];

    [HarmonyPrefix]
    public static void Prefix()
    {
        CGSCharacterSelectAudio.StopIntro();
    }
}
