using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Combat;
using ChevalGrandSlay.Mechanics;
using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSOldPactPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => new();

    private int _triggeredTurn = -1;
    private bool _pendingDraw;

    public override async Task AfterCardChangedPiles(CardModel card, PileType previousPile, AbstractModel? source)
    {
        if (Owner.Player is not { } player || card.Owner != player || previousPile == Entry.CGSFatePile ||
            card.Pile?.Type != Entry.CGSFatePile || !card.DynamicVars.ContainsKey("Uses") ||
            player.PlayerCombatState is not { } state || _triggeredTurn == state.TurnNumber)
            return;
        _triggeredTurn = state.TurnNumber;
        await PlayerCmd.GainEnergy(1m, player);
        _pendingDraw = Amount > 1m;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!_pendingDraw || Owner.Player is not { } player) return;
        _pendingDraw = false;
        await CardPileCmd.Draw(choiceContext, 1m, player);
    }

    public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<MegaCrit.Sts2.Core.Entities.Creatures.Creature> participants)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner) || !_pendingDraw ||
            Owner.Player is not { } player) return;
        _pendingDraw = false;
        await CardPileCmd.Draw(choiceContext, 1m, player);
    }
}
