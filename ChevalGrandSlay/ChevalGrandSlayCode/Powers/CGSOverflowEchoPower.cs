using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

// 能力：打向远方!
[RegisterPower]
public sealed class CGSOverflowEchoPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    // 自定义图标路径。1:1即可。原版游戏大图256x256，小图64x64。
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/small/CGSOverflowEcho.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/CGSOverflowEcho.png"
    );

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner || target?.IsEnemy != true || !result.WasTargetKilled)
            return;

        decimal damage = Math.Min(result.OverkillDamage, Amount);
        if (damage <= 0) return;

        var others = CombatState.HittableEnemies.Where(enemy => enemy != target && enemy.IsAlive).ToArray();
        if (others.Length == 0) return;

        await CreatureCmd.Damage(choiceContext, others, damage, ValueProp.Unpowered, Owner);
    }
}
