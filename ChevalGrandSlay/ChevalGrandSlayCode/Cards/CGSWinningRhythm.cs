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

namespace ChevalGrandSlay.Cards;

// 连胜步调
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSWinningRhythm : ModCardTemplate
{
    private const string PowerDamageVarName = "PowerDamage";

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(PowerDamageVarName, 3m)];

    public CGSWinningRhythm() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CGSWinningRhythmPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[PowerDamageVarName].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[PowerDamageVarName].UpgradeValueBy(1m);
    }
}

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
