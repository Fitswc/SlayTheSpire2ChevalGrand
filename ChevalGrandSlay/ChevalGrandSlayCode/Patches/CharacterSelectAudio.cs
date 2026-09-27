using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using STS2RitsuLib;
using STS2RitsuLib.Audio;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;

namespace ChevalGrandSlay;

internal static class CharacterSelectAudio
{
    private const string Intro =
        "event:/ChevalGrandSlaySFX/SFX/CGSSIntroduce";

    private const string Transition =
        "event:/ChevalGrandSlaySFX/SFX/CGSSTransition";

    private static AudioEventHandle? _introHandle;

    public static void Install()
    {
        var patcher = RitsuLibFramework.CreatePatcher(Entry.ModId, "CharacterSelectAudio");
        patcher.RegisterPatch<PlayIntroPatch>();
        patcher.RegisterPatch<StopIntroPatch>();

        if (!patcher.PatchAll())
            throw new InvalidOperationException("Failed to install character select audio patches.");
    }

    private sealed class PlayIntroPatch : IPatchMethod
    {
        public static string PatchId => "CGS_character_select_intro_play";
        public static string Description => "Control character introduction audio";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets() =>
        [
            PatchTarget.Method(
                typeof(SfxCmd),
                nameof(SfxCmd.Play),
                typeof(string), typeof(float))
        ];

        public static bool Prefix(string sfx, float volume) => BeforePlay(sfx, volume);
    }

    private sealed class StopIntroPatch : IPatchMethod
    {
        public static string PatchId => "character_select_intro_stop";
        public static string Description => "Stop introduction audio on screen actions";
        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets() =>
        [
            PatchTarget.Method<NCharacterSelectScreen>("SelectCharacter"),
            PatchTarget.Method<NCharacterSelectScreen>("OnLocalCharacterChangedForRandom"),
            PatchTarget.Method<NCharacterSelectScreen>("OnEmbarkPressed"),
            PatchTarget.Method<NCharacterSelectScreen>("OnSubmenuClosed")
        ];

        public static void Prefix() => StopIntro();
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