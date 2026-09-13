using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Relics;

// RegisterRelic 会把遗物注册进指定遗物池。
// RegisterCharacterStarterRelic 会把它作为 ChevalGrandSlayCharacter 的初始遗物。
[RegisterRelic(typeof(ChevalGrandSlayRelicPool))]
[RegisterCharacterStarterRelic(typeof(ChevalGrandSlayCharacter))]
public sealed class ChevalGrandSlayRelic : ModRelicTemplate
{
    // 稀有度。
    public override RelicRarity Rarity => RelicRarity.Common;
    private bool _hasGrantedEnergy = false;

    // 遗物的数值。这里会替换本地化中的 {Energy}。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1)
    ];

    // 图片资源统一放在 AssetProfile 里配置。
    // 三个路径可以先指向同一张图。后续有高清图或轮廓图时再拆开。
    public override RelicAssetProfile AssetProfile => new(
        // 小图标（原版 85x85）。
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        // 轮廓图标（原版 85x85）。
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        // 大图标（原版 256x256）。
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    // 回合开始时，进入防御姿态
    // 这里使用 DynamicVars.Energy.IntValue，保证效果和本地化显示保持一致。
    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is CombatRoom)
        {
            await PowerCmd.Apply<ChevalGrandSlayDefenseStancePower>(
                new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                1m,
                Owner.Creature,
                null
            );
        }
    }

    //首次切换姿态+1费
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        if (!_hasGrantedEnergy && power.Owner == Owner.Creature &&
            amount > 0 && cardSource != null && power is ChevalGrandSlayDefenseStancePower or ChevalGrandAttackStancePower)
        {
            _hasGrantedEnergy = true;
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _hasGrantedEnergy = false;
        return Task.CompletedTask;
    }
}