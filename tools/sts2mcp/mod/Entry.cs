using System.Diagnostics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace STS2MCP;

[ModInitializer(nameof(Initialize))]
public static class Entry
{
    private static GameBridge? bridge;
    private static (StartRunLobby Lobby, string Seed)? requestedSeed;

    internal static void SetEmbarkSeed(StartRunLobby lobby, string? seed)
        => requestedSeed = seed is null ? null : (lobby, SeedHelper.CanonicalizeSeed(seed));

    public static void Initialize()
    {
        var harmony = new Harmony("sts2mcp.local.bridge");
        try
        {
            harmony.PatchAll(typeof(Entry).Assembly);
            // Steam Workshop discovery can finish after Godot has already called NGame._Ready.
            if (NGame.Instance is { } game)
                Callable.From(() => Start(game)).CallDeferred();
        }
        catch (Exception e)
        {
            harmony.UnpatchAll(harmony.Id);
            GD.PushError($"STS2MCP could not attach to this game version: {e.Message}");
        }
    }

    [HarmonyPatch(typeof(StartRunLobby), nameof(StartRunLobby.SetReady))]
    private static class SeededEmbark
    {
        private static void Prefix(StartRunLobby __instance, bool ready, ref (NGame Game, string? Prior)? __state)
        {
            // First-run tutorial confirmation can call SetReady after the original Embark click returns.
            if (!ready || requestedSeed is not { } seed || seed.Lobby != __instance || NGame.Instance is not { } game)
                return;
            requestedSeed = null;
            __state = (game, game.DebugSeedOverride);
            game.DebugSeedOverride = seed.Seed;
        }

        private static void Finalizer((NGame Game, string? Prior)? __state)
        {
            if (__state is { } prior)
                prior.Game.DebugSeedOverride = prior.Prior;
        }
    }

    [HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]
    private static class StartBridge
    {
        private static void Postfix(NGame __instance) => Start(__instance);
    }

    private static void Start(NGame game)
    {
        if (bridge is not null || !GodotObject.IsInstanceValid(game) || !game.IsInsideTree())
            return;
        try
        {
            bridge = new GameBridge(game);
            game.TreeExiting += Stop;
        }
        catch (Exception e)
        {
            GD.PushError($"STS2MCP could not start: {e.Message}");
        }
    }

    private static void Stop()
    {
        bridge?.Dispose();
        bridge = null;
    }

    [HarmonyPatch(typeof(NClickableControl), "OnReleaseHandler")]
    private static class ManualClick
    {
        private static void Prefix(NClickableControl __instance, bool ____isPressed, ref ActionView? __state)
        {
            if (____isPressed && __instance is not NEndTurnButton)
                __state = bridge?.FindControl(__instance);
        }

        private static void Postfix(ActionView? __state) => bridge?.Observe(__state);
    }

    [HarmonyPatch(typeof(NEndTurnButton), nameof(NEndTurnButton.CallReleaseLogic))]
    private static class ManualEndTurn
    {
        private static void Prefix(NEndTurnButton __instance, ref ActionView? __state) => __state = bridge?.FindControl(__instance);
        private static void Postfix(ActionView? __state) => bridge?.Observe(__state);
    }

    [HarmonyPatch(typeof(NCardHolder), "EmitPressed")]
    private static class ManualCardSelection
    {
        private static void Prefix(NCardHolder __instance, ref ActionView? __state) => __state = bridge?.FindControl(__instance);
        private static void Postfix(ActionView? __state) => bridge?.Observe(__state);
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.TryManualPlay))]
    private static class ManualPlay
    {
        private static void Postfix(CardModel __instance, Creature? target, bool __result)
        {
            if (__result)
                bridge?.Observe(bridge.FindControl((__instance, target)) ??
                    new ActionView("manual:play_card", "play_card", __instance.Id.Entry,
                        new { target = target?.CombatId }));
        }
    }

    [HarmonyPatch(typeof(MerchantEntry), nameof(MerchantEntry.OnTryPurchaseWrapper))]
    private static class ManualPurchase
    {
        private static void Prefix(MerchantEntry __instance) => bridge?.Observe(bridge.FindControl(__instance));
    }

    [HarmonyPatch(typeof(PotionModel), nameof(PotionModel.EnqueueManualUse))]
    private static class ManualPotion
    {
        private static void Postfix(PotionModel __instance, Creature? target) =>
            bridge?.Observe(bridge.FindControl((__instance, target)) ??
                new ActionView("manual:use_potion", "use_potion", __instance.Id.Entry,
                    new { target = target?.CombatId }));
    }
}

internal sealed class GameBridge : IDisposable
{
    private readonly NGame game;
    private readonly SceneTree tree;
    private readonly BridgeState state = new();
    private readonly HttpBridge http;
    private GameFrame? frame;
    private ActionRequest? pending;
    private long pendingSince;
    private long lastCapture;
    private bool dispatching;
    private string? captureError;

    public GameBridge(NGame game)
    {
        this.game = game;
        tree = game.GetTree();
        string? configuredPort = System.Environment.GetEnvironmentVariable("STS2_MCP_PORT");
        int port = 15527;
        if (configuredPort is not null && (!int.TryParse(configuredPort, out port) || port is < 1024 or > 65535))
            throw new InvalidOperationException("STS2_MCP_PORT must be between 1024 and 65535.");
        http = new HttpBridge(state, port);
        tree.ProcessFrame += Tick;
        GD.Print($"STS2MCP listening on http://127.0.0.1:{port}/");
    }

    private void Tick()
    {
        ActionRequest? request = state.Take();
        if (request is null && Stopwatch.GetElapsedTime(lastCapture) < TimeSpan.FromMilliseconds(80))
            return;
        lastCapture = Stopwatch.GetTimestamp();
        long revision;
        try
        {
            frame = GameUi.Capture(game);
            revision = state.PublishState(frame.State);
            captureError = null;
        }
        catch (Exception e)
        {
            frame = null;
            string message = e.GetBaseException().Message;
            if (message != captureError)
                GD.PushWarning($"STS2MCP could not read the current screen: {message}");
            captureError = message;
            state.PublishState(new { screen = "unavailable", error = "game_state_unavailable", actions = Array.Empty<ActionView>() });
            if ((request ?? pending) is { } failed)
                state.Finish(failed, pending is null ? "rejected" : "unconfirmed", "game_state_unavailable");
            pending = null;
            return;
        }

        if (pending is { } running)
        {
            if (revision != running.Revision)
            {
                state.Finish(running, "state_changed");
                pending = null;
            }
            else if (Stopwatch.GetElapsedTime(pendingSince) >= TimeSpan.FromSeconds(5))
            {
                state.Finish(running, "unconfirmed", "no_state_change_observed");
                pending = null;
            }
        }
        if (request is not null)
            Execute(request, revision);
    }

    private void Execute(ActionRequest request, long revision)
    {
        if (request.Revision != revision)
        {
            state.Finish(request, "rejected", "stale_revision");
            return;
        }
        GameAction? action = frame?.Actions.FirstOrDefault(a => a.View.Id == request.ActionId);
        if (action is null || (request.Seed is not null && !action.View.AcceptsSeed))
        {
            state.Finish(request, "rejected", "illegal_action");
            return;
        }

        try
        {
            dispatching = true;
            if (action.Invoke(request.Seed))
            {
                pending = request;
                pendingSince = Stopwatch.GetTimestamp();
            }
            else
                state.Finish(request, "rejected", "game_rejected_action");
        }
        catch (Exception e)
        {
            GD.PushWarning($"STS2MCP action {action.View.Kind} failed: {e.GetBaseException().Message}");
            state.Finish(request, "unconfirmed", "dispatch_failed");
        }
        finally
        {
            dispatching = false;
        }
    }

    public ActionView? FindControl(object control) => frame?.Actions.FirstOrDefault(a => Equals(a.Control, control))?.View;

    public void Observe(ActionView? view)
    {
        if (!dispatching && frame?.Singleplayer == true && view is not null)
            state.Observe(view, "dispatched");
    }

    public void Dispose()
    {
        tree.ProcessFrame -= Tick;
        http.Dispose();
    }
}
