using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Ftue;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using MegaCrit.Sts2.Core.Runs;

namespace STS2MCP;

internal sealed record GameAction(ActionView View, Func<string?, bool> Invoke, object? Control = null);
internal sealed record GameFrame(object State, IReadOnlyList<GameAction> Actions, bool Singleplayer);

internal static class GameUi
{
    private static readonly AccessTools.FieldRef<NCardHolder, bool> IsCardHolderClickable =
        AccessTools.FieldRefAccess<NCardHolder, bool>("_isClickable");

    public static GameFrame Capture(NGame game)
    {
        var manager = RunManager.Instance;
        var run = manager.DebugOnlyGetState();
        var screen = ActiveScreenContext.Instance.GetCurrentScreen();
        bool singleplayer = run is null || manager.NetService?.Type == NetGameType.Singleplayer;
        var player = singleplayer ? LocalContext.GetMe(run) : null;
        var combat = player is null ? null : CombatManager.Instance.DebugOnlyGetState();
        var combatPlayer = player?.PlayerCombatState;
        var actions = new List<GameAction>();

        if (singleplayer && !game.GetTree().Paused && !manager.IsCleaningUp)
            CollectActions(screen, combat, actions);

        var state = new
        {
            screen = screen?.GetType().Name ?? "loading",
            singleplayer,
            run = run is null || player is null ? null : new
            {
                seed = run.Rng.StringSeed,
                act = run.CurrentActIndex + 1,
                floor = run.TotalFloor,
                room = run.CurrentRoom?.RoomType.ToString(),
                character = player.Character.Id.Entry,
                player.Gold,
                hp = player.Creature.CurrentHp,
                maxHp = player.Creature.MaxHp,
                player.Creature.Block,
                deck = player.Deck.Cards.Select(c => new { id = c.Id.Entry, name = c.Title, upgraded = c.IsUpgraded }).ToArray(),
                relics = player.Relics.Select(r => r.Id.Entry).ToArray(),
                potions = player.PotionSlots.Select(p => p?.Id.Entry).ToArray()
            },
            combat = combat is null || combatPlayer is null ? null : new
            {
                turn = combat.RoundNumber,
                side = combat.CurrentSide.ToString(),
                combatPlayer.Energy,
                combatPlayer.Stars,
                hand = combatPlayer.Hand.Cards.Select(DescribeCard).ToArray(),
                enemies = combat.Enemies.Select(e => DescribeEnemy(e, combat.PlayerCreatures)).ToArray(),
                drawCount = combatPlayer.DrawPile.Cards.Count,
                discardCount = combatPlayer.DiscardPile.Cards.Count
            },
            actions = actions.Select(a => a.View).ToArray()
        };
        return new GameFrame(state, actions, singleplayer);
    }

    private static void CollectActions(object? screen, CombatState? combat, List<GameAction> actions)
    {
        switch (screen)
        {
            case NEarlyAccessDisclaimer disclaimer:
                foreach (var button in Descendants<NDisclaimerProceedButton>(disclaimer))
                    AddButton(actions, button, "dismiss_early_access", ButtonLabel(button));
                break;
            case NMainMenu menu:
                AddButton(actions, menu.GetNodeOrNull<NClickableControl>("MainMenuTextButtons/SingleplayerButton"),
                    "open_singleplayer", "Singleplayer");
                AddButton(actions, menu.GetNodeOrNull<NClickableControl>("MainMenuTextButtons/ContinueButton"),
                    "continue_run", "Continue run");
                break;
            case NSingleplayerSubmenu menu:
                AddButton(actions, menu.GetNodeOrNull<NClickableControl>("StandardButton"), "open_standard", "Standard run");
                break;
            case NCharacterSelectScreen select when select.Lobby.NetService.Type == NetGameType.Singleplayer:
                foreach (var button in Descendants<NCharacterSelectButton>(select).Where(b => !b.IsLocked && !b.IsSelected))
                    AddButton(actions, button, "select_character", button.IsRandom ? "Random" : button.Character.Id.Entry);
                AddButton(actions, select.GetNodeOrNull<NClickableControl>("ConfirmButton"), "embark", "Embark",
                    beforeClick: seed => Entry.SetEmbarkSeed(select.Lobby, seed), acceptsSeed: true);
                break;
            case NFtue tutorial:
                foreach (var button in Descendants<NClickableControl>(tutorial).Where(b => b is NButton))
                    AddButton(actions, button, "tutorial", ButtonLabel(button));
                break;
            case NMapScreen map when map.IsTravelEnabled && !map.IsTraveling && map.Drawings.GetLocalDrawingMode() == DrawingMode.None:
                foreach (var point in Descendants<NMapPoint>(map).Where(p => p.State == MapPointState.Travelable))
                {
                    var coord = point.Point.coord;
                    AddButton(actions, point, "choose_map_node", point.Point.PointType.ToString(),
                        new { row = coord.row, column = coord.col });
                }
                break;
            case NCombatRoom room when combat is not null:
                CollectCombat(room, combat, actions);
                break;
            case NRewardsScreen rewards:
                foreach (var button in Descendants<NRewardButton>(rewards))
                    if (button.Reward is { } reward)
                        AddButton(actions, button, "claim_reward", reward.Description.GetFormattedText());
                AddProceed(actions, rewards);
                break;
            case NCardRewardSelectionScreen cards:
                AddCardSelection(actions, cards);
                foreach (var button in Descendants<NCardRewardAlternativeButton>(cards))
                    AddButton(actions, button, "reward_alternative", ButtonLabel(button));
                break;
            case NEventRoom room:
                foreach (var button in Descendants<NEventOptionButton>(room))
                    AddButton(actions, button, "event_option", button.Option.Title.GetFormattedText(),
                        new { description = button.Option.Description.GetFormattedText() });
                AddProceed(actions, room);
                break;
            case NRestSiteRoom room:
                foreach (var option in room.Options)
                    AddButton(actions, room.GetButtonForOption(option), "rest_option", option.Title.GetFormattedText(),
                        new { description = option.Description.GetFormattedText() });
                AddButton(actions, room.ProceedButton, "proceed", "Proceed");
                break;
            case NMerchantRoom room:
                AddButton(actions, room.MerchantButton, "open_shop", "Open shop");
                AddButton(actions, room.ProceedButton, "proceed", "Proceed");
                break;
            case NMerchantInventory inventory:
                AddShop(actions, inventory);
                break;
            case NTreasureRoom room:
                AddButton(actions, room.GetNodeOrNull<NClickableControl>("%Chest"), "open_chest", "Open chest");
                AddButton(actions, room.ProceedButton, "proceed", "Proceed");
                break;
            case NTreasureRoomRelicCollection relics:
                foreach (var holder in Descendants<NTreasureRoomRelicHolder>(relics))
                    AddButton(actions, holder, "claim_relic", ButtonLabel(holder));
                break;
            case Control selection when selection is NCardGridSelectionScreen or NChooseACardSelectionScreen:
                AddCardSelection(actions, selection);
                break;
        }
    }

    private static void CollectCombat(NCombatRoom room, CombatState combat, List<GameAction> actions)
    {
        var manager = CombatManager.Instance;
        var hand = room.Ui.Hand;
        if (hand.IsInCardSelection)
        {
            foreach (var holder in hand.ActiveHolders)
                AddHolder(actions, holder);
            AddSelectionButtons(actions, hand);
            return;
        }
        var me = LocalContext.GetMe(combat);
        if (me?.PlayerCombatState is not { } player || !manager.IsInProgress || manager.IsPaused
            || manager.PlayerActionsDisabled || manager.IsStarting || manager.IsEnding
            || combat.CurrentSide != CombatSide.Player || manager.IsPlayerReadyToEndTurn(me)
            || RunManager.Instance.ActionExecutor.IsRunning || hand.InCardPlay)
            return;

        var endTurn = room.Ui.EndTurnButton;
        if (endTurn.IsEnabled && endTurn.IsVisibleInTree())
        {
            var view = new ActionView($"end_turn:{endTurn.GetInstanceId()}", "end_turn", "End turn");
            actions.Add(new GameAction(view, _ =>
            {
                endTurn.CallReleaseLogic();
                return true;
            }, endTurn));
        }
        foreach (var card in player.Hand.Cards)
        {
            var holder = hand.GetCardHolder(card);
            if (holder is null || !holder.IsVisibleInTree() || !card.CanPlay())
                continue;
            if (card.IsValidTarget(null))
                AddPlay(actions, card, holder, null);
            foreach (var enemy in combat.HittableEnemies.Where(card.IsValidTarget))
                AddPlay(actions, card, holder, enemy);
        }
        for (int slot = 0; slot < me.PotionSlots.Count; slot++)
        {
            var potion = me.PotionSlots[slot];
            if (potion is null || potion.IsQueued || potion.Usage is not (PotionUsage.CombatOnly or PotionUsage.AnyTime)
                || !potion.PassesCustomUsabilityCheck)
                continue;
            if (potion.IsValidTarget(null))
                AddPotion(actions, potion, slot, null);
            else if (potion.IsValidTarget(me.Creature))
                AddPotion(actions, potion, slot, me.Creature);
            foreach (var enemy in combat.HittableEnemies.Where(potion.IsValidTarget))
                AddPotion(actions, potion, slot, enemy);
        }
    }

    private static void AddPotion(List<GameAction> actions, PotionModel potion, int slot, Creature? target)
    {
        var details = new
        {
            slot,
            id = potion.Id.Entry,
            description = potion.DynamicDescription.GetFormattedText(),
            target = target is null ? null : CreatureId(target)
        };
        var view = new ActionView($"use_potion:{slot}:{details.id}:{details.target}", "use_potion",
            potion.Title.GetFormattedText(), details);
        actions.Add(new GameAction(view, _ =>
        {
            potion.EnqueueManualUse(target);
            return true;
        }, (potion, target)));
    }

    private static void AddPlay(List<GameAction> actions, CardModel card, NCardHolder holder, Creature? target)
    {
        var details = new { card = DescribeCard(card), target = target is null ? null : CreatureId(target) };
        var view = new ActionView($"play_card:{holder.GetInstanceId()}:{details.target}", "play_card", card.Title, details);
        actions.Add(new GameAction(view, _ => card.TryManualPlay(target), (card, target)));
    }

    private static void AddShop(List<GameAction> actions, NMerchantInventory inventory)
    {
        AddButton(actions, inventory.GetNodeOrNull<NClickableControl>("%BackButton"), "close_shop", "Close shop");
        if (inventory.GetNodeOrNull<Control>("%InputBlocker")?.MouseFilter == Control.MouseFilterEnum.Stop)
            return;
        foreach (var slot in inventory.GetAllSlots())
        {
            if (!slot.IsVisibleInTree() || !slot.Entry.IsStocked || !slot.Entry.EnoughGold)
                continue;
            string label = slot.Entry switch
            {
                MerchantCardEntry card => card.CreationResult?.Card.Title ?? "Card",
                MerchantRelicEntry relic => relic.Model!.Id.Entry,
                MerchantPotionEntry potion => potion.Model!.Id.Entry,
                _ => "Remove a card"
            };
            var view = new ActionView($"buy:{slot.GetInstanceId()}", "buy", label, new { cost = slot.Entry.Cost });
            actions.Add(new GameAction(view, _ =>
            {
                using var input = new InputEventAction { Action = MegaInput.select, Pressed = true };
                slot._GuiInput(input);
                return true;
            }, slot.Entry));
        }
    }

    private static void AddCardSelection(List<GameAction> actions, Control screen)
    {
        Control? grid = screen is NCardGridSelectionScreen ? screen.GetNodeOrNull<Control>("%CardGrid") : screen;
        if (grid is not null && grid.FocusBehaviorRecursive != Control.FocusBehaviorRecursiveEnum.Disabled)
            foreach (var holder in Descendants<NCardHolder>(grid))
                AddHolder(actions, holder);
        AddSelectionButtons(actions, screen);
    }

    private static void AddSelectionButtons(List<GameAction> actions, Control screen)
    {
        foreach (var button in Descendants<NClickableControl>(screen).Where(b =>
                     b.Name.ToString() is "ConfirmButton" or "Confirm" or "SkipButton" or "CancelButton" or "Cancel"
                         or "BackButton" or "SelectModeConfirmButton" or "PreviewCancel" or "PreviewConfirm"))
            AddButton(actions, button, "selection_button", ButtonLabel(button));
    }

    private static void AddHolder(List<GameAction> actions, NCardHolder holder)
    {
        if (holder is NPreviewCardHolder || !holder.IsVisibleInTree() || holder.Hitbox?.IsEnabled != true || !IsCardHolderClickable(holder)
            || holder.CardModel is not { } card)
            return;
        var view = new ActionView($"select_card:{holder.GetInstanceId()}", "select_card", card.Title, DescribeCard(card));
        actions.Add(new GameAction(view, _ => holder.EmitSignal(NCardHolder.SignalName.Pressed, holder) == Error.Ok, holder));
    }

    private static void AddProceed(List<GameAction> actions, Node room)
    {
        foreach (var button in Descendants<NClickableControl>(room).Where(b => b.Name.ToString() == "ProceedButton"))
            AddButton(actions, button, "proceed", ButtonLabel(button));
    }

    private static void AddButton(List<GameAction> actions, NClickableControl? button, string kind, string label,
        object? details = null, Action<string?>? beforeClick = null, bool acceptsSeed = false)
    {
        if (button is null || !button.IsEnabled || !button.IsVisibleInTree())
            return;
        var view = new ActionView($"{kind}:{button.GetInstanceId()}", kind, label, details, acceptsSeed);
        actions.Add(new GameAction(view, seed =>
        {
            beforeClick?.Invoke(seed);
            button.ForceClick();
            return true;
        }, button));
    }

    private static string ButtonLabel(Control button)
    {
        string? text = Descendants<RichTextLabel>(button).Select(n => n.Text)
            .Concat(Descendants<Label>(button).Select(n => n.Text)).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));
        return text ?? button.Name.ToString();
    }

    private static IEnumerable<T> Descendants<T>(Node parent) where T : Node
    {
        foreach (Node child in parent.GetChildren())
        {
            if (child is T match)
                yield return match;
            foreach (T descendant in Descendants<T>(child))
                yield return descendant;
        }
    }

    private static object DescribeCard(CardModel card) => new
    {
        id = card.Id.Entry,
        name = card.Title,
        description = card.GetDescriptionForPile(card.Pile?.Type ?? PileType.Deck),
        energy = card.EnergyCost.CostsX ? (int?)null : card.EnergyCost.GetResolved(),
        stars = card.GetStarCostWithModifiers(),
        upgraded = card.IsUpgraded
    };

    private static string CreatureId(Creature creature) => creature.CombatId?.ToString() ?? creature.Monster?.Id.Entry ?? "player";

    private static object DescribeEnemy(Creature enemy, IEnumerable<Creature> players) => new
    {
        id = CreatureId(enemy),
        name = enemy.Monster?.Title.GetFormattedText(),
        hp = enemy.CurrentHp,
        maxHp = enemy.MaxHp,
        enemy.Block,
        intents = enemy.Monster?.NextMove.Intents.Select(intent => new
        {
            type = intent.IntentType.ToString(),
            label = intent.GetIntentLabel(players, enemy).GetFormattedText()
        }).ToArray()
    };
}
