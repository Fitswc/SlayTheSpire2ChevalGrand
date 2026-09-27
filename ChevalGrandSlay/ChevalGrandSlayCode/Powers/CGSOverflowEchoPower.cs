using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSOverflowEchoPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new();

    private bool _usedThisTurn;

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        _usedThisTurn = false;
        return Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (_usedThisTurn || dealer != Owner || target?.IsEnemy != true || !result.WasTargetKilled)
            return;
        _usedThisTurn = true;
        if (result.OverkillDamage <= 0 || CombatState is null) return;

        var others = CombatState.HittableEnemies.Where(enemy => enemy != target).ToArray();
        if (others.Length == 0) return;
        var other = Owner.Player?.RunState.Rng.CombatTargets.NextItem(others);
        if (other is null) return;

        decimal damage = Math.Min(result.OverkillDamage, Amount);
        await CreatureCmd.Damage(choiceContext, other, damage, ValueProp.Unpowered, Owner, null);
    }
}
