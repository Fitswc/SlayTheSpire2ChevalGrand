using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Combat;
using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSWinningRhythmPower : ModPowerTemplate
{
    private int _triggeredTurn = -1;
    private int _timesThisTurn;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new();

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner.Player;
        if (player?.PlayerCombatState is not { } state ||
            CombatState?.CurrentSide != CombatSide.Player ||
            cardPlay.Card.Owner != player || cardPlay.Card.Type != CardType.Attack)
            return;

        if (_triggeredTurn != state.TurnNumber)
        {
            _triggeredTurn = state.TurnNumber;
            _timesThisTurn = 0;
        }
        if (_timesThisTurn >= 3) return;
        _timesThisTurn++;
        Flash();
        foreach (var enemy in CombatState.HittableEnemies.ToArray())
        {
            if (enemy.IsAlive)
                await CreatureCmd.Damage(choiceContext, enemy, Amount, ValueProp.Unpowered, Owner, null);
        }
    }
}
