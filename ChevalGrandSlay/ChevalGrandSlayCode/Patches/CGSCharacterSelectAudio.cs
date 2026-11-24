using MegaCrit.Sts2.Core.Combat;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using STS2RitsuLib.Audio;
using STS2RitsuLib.Patching.Models;

namespace ChevalGrandSlay;

internal sealed class CGSCharacterSelectAudio : IPatchMethod
{
    public static string PatchId => "CGSCharacterSelectAudio";

    // 拦截接收事件路径和音量的 SfxCmd.Play 重载，以便持有介绍语音的播放句柄。
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(SfxCmd), nameof(SfxCmd.Play), [typeof(string), typeof(float)])];

    private const string Intro = "event:/ChevalGrandSlaySFX/SFX/CGSSIntroduce";
    private const string Transition = "event:/ChevalGrandSlaySFX/SFX/CGSSTransition";

    private static AudioEventHandle? _introHandle;

    public static bool Prefix([HarmonyArgument(0)] string sfx, [HarmonyArgument(1)] float volume)
    {
        if (sfx != Intro && sfx != Transition)
            return true;

        // 新的介绍或转场开始前，先停止上一次介绍。多人同步出发时也会播放转场音效。
        StopIntro();

        if (sfx == Transition)
            return true;

        // 非交互模式或战斗结束过程中，跳过介绍语音。
        if (NonInteractiveMode.IsActive || CombatManager.Instance.IsEnding)
            return false;

        // 由补丁创建并持有 FMOD 实例。启动失败时及时清理，方便下次播放。
        _introHandle = FmodStudioEventInstances.TryCreateHandle(
            AudioSource.Event(Intro), new AudioPlaybackOptions());
        if (_introHandle == null)
        {
            Entry.Logger.Warn("The character introduction audio instance CANNOT be created.");
            return false;
        }

        _introHandle.TrySetVolume(volume);

        if (!_introHandle.TryPlay())
            StopIntro();

        // 自我介绍由本补丁播放，跳过原方法，避免重复播放。
        return false;
    }

    // 先清空缓存的句柄，再立即停止并释放实例。多处界面回调可以重复请求停止。
    internal static void StopIntro()
    {
        var handle = _introHandle;
        _introHandle = null;

        handle?.TryStop(allowFadeOut: false);
        handle?.TryRelease();
    }
}

// 切换角色、出发或关闭子菜单时，停止尚未播完的自我介绍。
internal sealed class CGSCharacterSelectStopIntroPatch : IPatchMethod
{
    public static string PatchId => "character_select_stop_intro";

    public static ModPatchTarget[] GetTargets()
    {
        // 随机切换角色的回调在不同游戏版本中使用不同名称，按当前版本选择补丁目标。
        var screen = typeof(NCharacterSelectScreen);
        var randomCharacterMethod = AccessTools.DeclaredMethod(screen, "DoRandomCharacterChange") is not null
            ? "DoRandomCharacterChange"
            : "OnLocalCharacterChangedForRandom";
        return
        [
            new(screen, "SelectCharacter"),
            new(screen, randomCharacterMethod),
            new(screen, "OnEmbarkPressed"),
            new(screen, "OnSubmenuClosed")
        ];
    }

    public static void Prefix()
    {
        CGSCharacterSelectAudio.StopIntro();
    }
}
