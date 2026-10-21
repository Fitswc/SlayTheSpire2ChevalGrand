using MegaCrit.Sts2.Core.Combat;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using STS2RitsuLib.Audio;

namespace ChevalGrandSlay;

[HarmonyPatch]
internal static class CGSCharacterSelectAudio
{
    private const string Intro = "event:/ChevalGrandSlaySFX/SFX/CGSSIntroduce";

    private const string Transition = "event:/ChevalGrandSlaySFX/SFX/CGSSTransition";

    private static AudioEventHandle? _introHandle;

    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods() =>
    [
        AccessTools.Method(typeof(SfxCmd), nameof(SfxCmd.Play), [typeof(string), typeof(float)]),
        AccessTools.Method(typeof(NCharacterSelectScreen), "SelectCharacter"),
        AccessTools.Method(typeof(NCharacterSelectScreen), "OnLocalCharacterChangedForRandom"),
        AccessTools.Method(typeof(NCharacterSelectScreen), "OnEmbarkPressed"),
        AccessTools.Method(typeof(NCharacterSelectScreen), "OnSubmenuClosed")
    ];

    [HarmonyPrefix]
    public static bool Prefix(MethodBase __originalMethod, object[] __args)
    {
        // The targets have different signatures, so route using Harmony's shared injected arguments.
        if (__originalMethod.DeclaringType == typeof(SfxCmd))
            return BeforePlay((string)__args[0], (float)__args[1]);

        StopIntro();
        return true;
    }

    private static bool BeforePlay(string sfx, float volume)
    {
        // 转场开始前再清理一次，也覆盖多人同步出发。
        if (sfx == Transition)
        {
            StopIntro();
            return true;
        }

        // 其他音效继续使用游戏原来的逻辑。
        if (sfx != Intro)
            return true;

        StopIntro();

        // 保留 SfxCmd 原有的播放条件。
        if (NonInteractiveMode.IsActive || CombatManager.Instance.IsEnding)
            return false;

        _introHandle = FmodStudioEventInstances.TryCreateHandle(
            AudioSource.Event(sfx), new AudioPlaybackOptions());
        if (_introHandle == null)
        {
            Entry.Logger.Info("The character introduction audio instance cannot be created.");
            return false;
        }

        _introHandle.TrySetVolume(volume);

        if (!_introHandle.TryPlay())
            StopIntro();

        // 禁止原方法再次播放，避免同一句播放两次。
        return false;
    }

    private static void StopIntro()
    {
        var handle = _introHandle;
        _introHandle = null;

        if (handle == null)
            return;

        handle.TryStop(allowFadeOut: false);
        handle.TryRelease();
    }
}
