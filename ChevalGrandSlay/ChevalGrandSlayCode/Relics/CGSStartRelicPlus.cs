using System.Runtime.CompilerServices;
using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Relics;

// RegisterRelic 会把遗物注册进指定遗物池。
// RegisterCharacterStarterRelic 会把它作为 ChevalGrandSlayCharacter 的初始遗物。
[RegisterRelic(typeof(CGSRelicPool))]
public sealed class CGSStartRelicPlus : ModRelicTemplate
{
    private static readonly ConditionalWeakTable<CardModel, object> StaminaBonuses = new();

    // 稀有度。
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    // 遗物的数值。这里会替换本地化中的 {Energy}。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new DynamicVar("BonusStamina", 1m)
    ];

    // 图片资源统一放在 AssetProfile 里配置。
    // 三个路径可以先指向同一张图。后续有高清图或轮廓图时再拆开。
    public override RelicAssetProfile AssetProfile => new(
        // 小图标（原版 85x85）。
        IconPath: $"{Entry.ResPath}/images/relics/CGSStartRelicPlus_small.png",
        // 轮廓图标（原版 85x85）。
        IconOutlinePath: $"{Entry.ResPath}/images/relics/CGSStartRelicPlus_outline.png",
        // 大图标（原版 256x256）。
        BigIconPath: $"{Entry.ResPath}/images/relics/CGSStartRelicPlus.png");

    public override Task BeforeCombatStart()
    {
        if (Owner.PlayerCombatState is { } combatState)
        {
            // Only change combat copies, so the bonus does not accumulate between battles.
            foreach (var card in combatState.AllCards.ToArray())
                ApplyStaminaBonus(card);
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        ApplyStaminaBonus(card);
        return Task.CompletedTask;
    }

    private void ApplyStaminaBonus(CardModel card)
    {
        if (card.Owner != Owner || !card.CanonicalKeywords.Contains(CGSKeywords.Stamina) ||
            StaminaBonuses.TryGetValue(card, out _) ||
            !card.DynamicVars.TryGetValue("Uses", out var uses) ||
            !card.DynamicVars.TryGetValue("MaxUses", out var maxUses))
            return;

        var bonus = DynamicVars["BonusStamina"].BaseValue;
        uses.BaseValue += bonus;
        maxUses.BaseValue += bonus;
        StaminaBonuses.Add(card, new object());
    }

    internal static void InheritStaminaBonus(CardModel original, CardModel copy)
    {
        // The copy patch already transfers Uses and MaxUses; transfer the marker too.
        if (StaminaBonuses.TryGetValue(original, out _) && !StaminaBonuses.TryGetValue(copy, out _))
            StaminaBonuses.Add(copy, new object());
    }

    // 回合开始时能量已重置，只在每场战斗的第一回合给予额外能量。
    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != CombatSide.Player ||
            !participants.Contains(Owner.Creature) ||
            Owner.PlayerCombatState?.TurnNumber != 1)
            return;

        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }
}
