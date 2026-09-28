using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSRestartTheJourneyPower : ModPowerTemplate
{
    private int _triggerCount;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new();

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || _triggerCount >= Amount) return;
        var candidates = Entry.CGSFatePile.GetPile(player).Cards
            .Where(card => card.DynamicVars.ContainsKey("Uses")).ToList();
        if (candidates.Count == 0) return;
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, candidates, player,
            new CardSelectorPrefs(SelectionScreenPrompt, 1));
        if (selected.FirstOrDefault() is not { } card) return;
        var uses = card.DynamicVars["Uses"];
        uses.BaseValue = Math.Min(card.DynamicVars["MaxUses"].BaseValue, uses.BaseValue + 1m);
        await CardPileCmd.Add(card, PileType.Discard);
        _triggerCount++;
    }
}
