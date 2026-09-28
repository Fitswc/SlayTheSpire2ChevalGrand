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
public sealed class CGSTurnTogetherPower : ModPowerTemplate
{
    private int _pendingDraws;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new();

    public override Task AfterCardChangedPiles(CardModel card, PileType previousPile, AbstractModel? source)
    {
        if (card.Owner == Owner.Player && previousPile == Entry.CGSFatePile &&
            card.Pile?.Type != Entry.CGSFatePile && CombatState?.CurrentSide == CombatSide.Player)
            _pendingDraws++;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        await ResolvePending(choiceContext);

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player) await ResolvePending(choiceContext);
    }

    public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player && participants.Contains(Owner))
            await ResolvePending(choiceContext);
    }

    private async Task ResolvePending(PlayerChoiceContext choiceContext)
    {
        while (_pendingDraws-- > 0)
        {
            await CardPileCmd.Draw(choiceContext, 1m, Owner.Player!);
            if (Amount > 0m)
                await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
        }
        _pendingDraws = 0;
    }
}
