using ChevalGrandSlay.Keywords;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.RestSiteOperation;

public sealed class CGSForgetOperation(Player owner) : ModRestSiteOptionTemplate(owner)
{
    private const int MaxCardsToRemove = 4;

    public override string OptionId => "CGS_FORGET";

    public override RestSiteOptionAssetProfile AssetProfile =>
        new(IconPath: "res://images/ui/rest_site/option_toke.png");

    public override bool IsEnabled =>
        PileType.Deck.GetPile(Owner).Cards.Any(card => card.IsRemovable && HasStamina(card));

    public override async Task<bool> OnSelect()
    {
        var prompt = new LocString("rest_site_ui", $"OPTION_{OptionId}.prompt");
        var preferences = new CardSelectorPrefs(prompt, minCount: 1, maxCount: MaxCardsToRemove)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        var selectedCards = (await CardSelectCmd.FromDeckForRemoval(Owner, preferences, HasStamina)).ToList();
        if (selectedCards.Count == 0)
            return false;

        await CardPileCmd.RemoveFromDeck(selectedCards);
        return true;
    }

    private static bool HasStamina(CardModel card) => card.Keywords.Contains(CGSKeywords.Stamina);
}
