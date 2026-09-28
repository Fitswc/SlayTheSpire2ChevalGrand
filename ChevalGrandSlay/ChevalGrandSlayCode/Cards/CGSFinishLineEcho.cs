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

namespace ChevalGrandSlay.Cards;

// 终线回声
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSFinishLineEcho : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new();
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("EchoDamage", 5m)];

    public CGSFinishLineEcho() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var power = await PowerCmd.Apply<CGSFinishLineEchoPower>(choiceContext, Owner.Creature,
            DynamicVars["EchoDamage"].BaseValue, Owner.Creature, this);
        power?.SetTurns(IsUpgraded ? 3 : 2);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["EchoDamage"].UpgradeValueBy(3m);
    }
}

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
