using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;


[RegisterPower]
public sealed class CGSFinishLineEchoPower : ModPowerTemplate
{
    private int _turnsRemaining;
    private int _pendingHits;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new();
    public void SetTurns(int turns) => _turnsRemaining = turns;

    public override Task AfterCardChangedPiles(CardModel card, PileType previousPile, AbstractModel? source)
    {
        if (card.Owner == Owner.Player && previousPile != Entry.CGSFatePile &&
            card.Pile?.Type == Entry.CGSFatePile && CombatState?.CurrentSide == CombatSide.Player)
            _pendingHits++;
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
        while (_pendingHits-- > 0)
        {
            var enemies = CombatState?.HittableEnemies;
            if (enemies is not { Count: > 0 }) continue;
            var enemy = Owner.Player?.RunState.Rng.CombatTargets.NextItem(enemies);
            if (enemy is not null)
                await CreatureCmd.Damage(choiceContext, enemy, Amount, ValueProp.Unpowered, Owner, null);
        }
        _pendingHits = 0;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner)) return;
        _turnsRemaining--;
        if (_turnsRemaining <= 0) await PowerCmd.Remove(this);
    }
}
