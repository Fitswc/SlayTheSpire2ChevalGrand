using ChevalGrandSlay.Keywords;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.RestSiteOperation;

public sealed class CGSForgetOperation(Player owner) : ModRestSiteOptionTemplate(owner)
{
    public override string OptionId => "CGS_FORGET";

    public override RestSiteOptionAssetProfile AssetProfile =>
        new(IconPath: "res://images/ui/rest_site/option_toke.png");

    public override bool IsEnabled =>
        PileType.Deck.GetPile(Owner).Cards.Any(card => card.IsRemovable && HasStamina(card));

    public override async Task<bool> OnSelect()
    {
        var preferences = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        var selectedCards = await CardSelectCmd.FromDeckForRemoval(Owner, preferences, HasStamina);
        var card = selectedCards.SingleOrDefault();
        if (card is null)
            return false;

        await CardPileCmd.RemoveFromDeck(card);
        return true;
    }

    private static bool HasStamina(CardModel card) => card.Keywords.Contains(CGSKeywords.Stamina);
}
