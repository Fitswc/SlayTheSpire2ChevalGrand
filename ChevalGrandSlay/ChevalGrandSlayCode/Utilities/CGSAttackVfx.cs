using MegaCrit.Sts2.Core.Commands.Builders;

namespace ChevalGrandSlay.Utilities;

public static class CGSAttackVfx
{
    /// <summary>设置命中特效路径；群攻时在每个目标中心播放。</summary>
    public static AttackCommand CGSWithVfx(this AttackCommand attack, string vfx)
    {
        return attack
            .WithHitFx(vfx: vfx)
            .SpawningHitVfxOnEachCreature();
    }
}
