using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using ChevalGrandSlay.Relics;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;
using MegaCrit.Sts2.Core.TestSupport;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.RestSiteOperation;

public sealed class CGSForgetOperation(Player owner) : ModRestSiteOptionTemplate(owner)
{
    private const int MaxCards = 4;
    private bool LocksCards => Owner.Relics.OfType<CGSVegatableJuice>().Any();

    public override string OptionId => LocksCards ? "CGS_FORGET_LOCK" : "CGS_FORGET";

    public override RestSiteOptionAssetProfile AssetProfile =>
        new(IconPath: "res://images/ui/rest_site/option_toke.png");

    public override bool IsEnabled =>
        PileType.Deck.GetPile(Owner).Cards.Any(LocksCards ? CGSStamina.CanRecoverStamina :
            card => card.IsRemovable && HasStamina(card));

    public override async Task<bool> OnSelect()
    {
        var prompt = new LocString("rest_site_ui", $"OPTION_{OptionId}.prompt");
        var preferences = new CardSelectorPrefs(prompt, minCount: 1, maxCount: MaxCards)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        if (LocksCards)
        {
            var cards = await CardSelectCmd.FromDeckGeneric(Owner, preferences, CGSStamina.CanRecoverStamina);
            bool lockedAny = false;
            foreach (var card in cards)
            {
                if (card.Pile?.Type != PileType.Deck || !CGSStamina.CanRecoverStamina(card))
                    continue;

                CGSStamina.LockDeckCard(card);
                lockedAny = true;
                PlayLockVfx(card);
            }
            return lockedAny;
        }

        var selectedCards = (await CardSelectCmd.FromDeckForRemoval(Owner, preferences, HasStamina)).ToList();
        if (selectedCards.Count == 0)
            return false;

        await CardPileCmd.RemoveFromDeck(selectedCards);
        return true;
    }

    private static void PlayLockVfx(CardModel card)
    {
        if (TestMode.IsOn || !LocalContext.IsMine(card) || NRun.Instance?.GlobalUi is not { } ui ||
            NCard.Create(card) is not { } preview)
            return;

        ui.CardPreviewContainer.AddChildSafely(preview);
        preview.UpdateVisuals(PileType.None, CardPreviewMode.Normal);

        var tween = preview.CreateTween();
        tween.TweenProperty(preview, "scale", Vector2.One, 0.25).From(Vector2.Zero)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tween.TweenInterval(0.25);
        tween.TweenCallback(Callable.From(() =>
        {
            if (NCardExhaustVfx.Create(preview) is { } vfx)
            {
                ui.AboveTopBarVfxContainer.AddChildSafely(vfx);
                TaskHelper.RunSafely(vfx.PlayAnimation());
            }
            else
            {
                preview.QueueFreeSafely();
            }
        }));
    }

    private static bool HasStamina(CardModel card) => card.Keywords.Contains(CGSKeywords.Stamina);
}
