using ChevalGrandSlay.Keywords;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

// 能力：疲惫的倔强
[RegisterPower]
public sealed class CGSRemainingUsesSensePower : ModPowerTemplate
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModKeywordRegistry.CreateHoverTip(CGSKeywords.RestPile)
    ];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    // 自定义图标路径。1:1即可。原版游戏大图256x256，小图64x64。
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/small/CGSRemainingUsesSense.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/CGSRemainingUsesSense.png"
    );

    public override async Task AfterCardChangedPiles(CardModel card, PileType previousPile, AbstractModel? source)
    {
        if (Owner.Player is not { } player || card.Owner != player || previousPile == Entry.CGSFatePile ||
            card.Pile?.Type != Entry.CGSFatePile || CombatState.CurrentSide != CombatSide.Player)
            return;

        var enemies = CombatState.HittableEnemies.Where(enemy => enemy.IsAlive).ToArray();
        if (enemies.Length == 0) return;

        var enemy = player.RunState.Rng.CombatTargets.NextItem(enemies);
        if (enemy is null) return;

        await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), enemy, Amount, ValueProp.Unpowered, Owner);
    }
}
