using global::AutoAnthony;
using ChevalGrandSlay.Cards;
using ChevalGrandSlay.Enchantments;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using ChevalGrandSlay.Powers;
using ChevalGrandSlay.Utilities;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace ChevalGrandSlay.Compatibility.AutoAnthony;

internal static partial class ChevalComponents
{
    private static async Task<bool> ExecuteAdditionalAsync(ComponentRuntimeContext context)
    {
        var source = context.Card;
        var player = source.Owner;
        var owner = player.Creature;
        var choice = context.ChoiceContext;
        var combat = source.CombatState;
        int amount = Math.Max(0, context.Amount);
        string variant = context.RuntimeSpec.Variant;

        switch (variant)
        {
            case ChainDiscount:
                return true; // Managed by the card's combat hooks while it is in a pile.
            case "resist_weak_frail":
                await Apply<CGSResistWeakFrailPower>(context, amount);
                return true;
            case "energy_if_entered_rest":
                if (source is ChevalChaosCard { RestEnteredThisTurn: true })
                    await PlayerCmd.GainEnergy(amount, player);
                return true;
            case "draw_if_entered_rest":
                if (source is ChevalChaosCard { RestEnteredThisTurn: true })
                    await DrawAsync(context, amount);
                return true;
            case "stun_target":
                if (context.Target is { IsMonster: true, IsAlive: true } monster)
                    await CreatureCmd.Stun(monster);
                return true;
            case "unpowered_all_damage":
                if (combat is not null)
                    await CreatureCmd.Damage(choice, combat.HittableEnemies, amount, ValueProp.Unpowered,
                        owner, source, context.CardPlay);
                return true;
            case Domain:
                if (!owner.HasPower<CGSDomainPower>())
                    await Apply<CGSDomainPower>(context, amount);
                return true;
            case "overflow_on_kill":
                await Apply<CGSOverflowEchoPower>(context, 999);
                return true;
            case FinalKick:
                if (source is ChevalChaosCard { CanUseFinalKick: true })
                    await AttackAllAsync(context, FinalKickDamage);
                return true;
            case "area_damage_on_rest":
                await Apply<CGSFinishLineEchoPower>(context, amount);
                return true;
            case "draw_after_fourth_attack":
                await Apply<CGSFlowingAssaultPower>(context, amount);
                return true;
            case "third_attack_reward":
                await Apply<CGSForgeAheadPower>(context, 1);
                return true;
            case RetainedDamage:
                int bonus = source is ChevalChaosCard { RetainedLastTurn: true } ? Value(context, "bonus") : 0;
                await AttackAsync(context, amount + bonus);
                return true;
            case "enemy_hp_loss_cap":
                await Apply<CGSEnemyAttackLossCapPower>(context, amount);
                return true;
            case "counter_full_block":
                await Apply<CGSFullBlockCounterPower>(context, amount);
                return true;
            case "rest_boost_next_attack":
                await Apply<CGSNoRegretsEndingPower>(context, amount);
                return true;
            case "block_on_rest":
                await Apply<CGSOldRoadWarmthPower>(context, amount);
                return true;
            case "hits_per_resting_card":
                await AttackAsync(context, amount, Entry.CGSFatePile.GetPile(player).Cards.Count);
                return true;
            case "extra_turn":
                await Apply<CGSOneBeatAheadPower>(context, 1);
                return true;
            case "perfect_defend":
                await Apply<CGSPerfectDefendPower>(context, amount);
                return true;
            case "block_next_turn":
                if (combat is not null)
                {
                    decimal block = Hook.ModifyBlock(combat, owner, amount, ValueProp.Move, source, context.CardPlay, out _);
                    await Apply<BlockNextTurnPower>(context, block);
                }
                return true;
            case "three_hits":
                for (int hit = 0; hit < 3; hit++)
                    await AttackAsync(context, amount);
                return true;
            case "cleanse_damage":
                int removed = 0;
                int limit = SelectionLimit(context, Value(context, "limit"));
                for (int i = 0; i < limit; i++)
                {
                    var debuff = owner.Powers.FirstOrDefault(power => power.Type == PowerType.Debuff && power.Amount > 0);
                    if (debuff is null)
                        break;
                    int previous = debuff.Amount;
                    await PowerCmd.ModifyAmount(choice, debuff, -1m, owner, source);
                    removed += Math.Max(0, previous - Math.Max(0, debuff.Amount));
                }

                await AttackAllAsync(context, (decimal)amount * removed);
                return true;
            case "random_damage_on_rest":
                await Apply<CGSRemainingUsesSensePower>(context, amount);
                return true;
            case "energy_strength_each_turn":
                await Apply<CGSOldPactPower>(context, source.IsUpgraded ? 2 : 1);
                return true;
            case "take_enemy_block":
                if (context.Target is { IsAlive: true } enemy)
                {
                    decimal removedBlock = Math.Min(enemy.Block, amount);
                    if (removedBlock > 0m)
                    {
                        decimal before = enemy.Block;
                        await CreatureCmd.LoseBlock(choice, enemy, removedBlock, null);
                        await CreatureCmd.GainBlock(owner, Math.Max(0m, before - enemy.Block), ValueProp.Move, context.CardPlay);
                    }
                    else if (enemy.Block == 0m)
                    {
                        await CreatureCmd.Damage(choice, enemy, 12m, ValueProp.Unblockable, owner, source, context.CardPlay);
                    }
                }

                return true;
            case "draw_when_no_stamina_in_hand":
                if (!PileType.Hand.GetPile(player).Cards.Any(card => card != source && CGSStamina.HasAtLeast(card, 1)))
                    await DrawAsync(context, amount);
                return true;
            case "double_current_block":
                await CreatureCmd.GainBlock(owner, owner.Block, ValueProp.Move | ValueProp.Unpowered, context.CardPlay);
                return true;
            case "damage_equal_to_block":
                await AttackAllAsync(context, owner.Block);
                return true;
            case "block_positive_power":
                if (context.Target is { IsAlive: true } target)
                    await PowerCmd.Apply<CGSNoPositivePower>(choice, target, 1m, owner, source);
                return true;
            case "damage_and_draw_against_debuff":
                if (context.Target is { IsAlive: true } debuffedTarget)
                {
                    bool shouldDraw = debuffedTarget.GetPower<VulnerablePower>() is { Amount: > 0 }
                        || debuffedTarget.GetPower<WeakPower>() is { Amount: > 0 };
                    await AttackAsync(context, amount);
                    if (shouldDraw)
                        await DrawAsync(context, Value(context, "draw"));
                }
                return true;
            case "draw_on_leave_rest":
                if (!owner.HasPower<CGSTurnTogetherUpgradedPower>())
                {
                    var current = owner.GetPower<CGSTurnTogetherPower>();
                    if (source.IsUpgraded)
                    {
                        if (current is not null)
                            await PowerCmd.Remove(current);
                        await Apply<CGSTurnTogetherUpgradedPower>(context, 1);
                    }
                    else if (current is null)
                    {
                        await Apply<CGSTurnTogetherPower>(context, 1);
                    }
                }
                return true;
            case "recover_first_leaving_rest":
                await Apply<CGSFillTheGapPower>(context, source.IsUpgraded ? 2 : 1);
                return true;
            case "block_on_costly_attack":
                await Apply<CGSUnwaveringStepPower>(context, amount);
                return true;
            case "block_on_skill":
                await Apply<CGSWinningRhythmPower>(context, amount);
                return true;
            default:
                return await ExecuteCardMovementAsync(context, amount);
        }
    }

    private static Task<T?> Apply<T>(ComponentRuntimeContext context, decimal amount) where T : PowerModel
    {
        var owner = context.Card.Owner.Creature;
        return PowerCmd.Apply<T>(context.ChoiceContext, owner, amount, owner, context.Card);
    }

    private static int Value(ComponentRuntimeContext context, string slot) =>
        context.RuntimeSpec.Values.Single(value => value.Id == slot).BaseValue;

    private static int SelectionLimit(ComponentRuntimeContext context, int fixedAmount) =>
        context.Card is ChevalChaosCard { EnergyCost.CostsX: true } source
            ? Math.Max(0, source.GetEnergyX(context.CardPlay))
            : fixedAmount;

    private static async Task DrawAsync(ComponentRuntimeContext context, int count)
    {
        if (count > 0)
            context.ReplaceDrawnCards(await CardPileCmd.Draw(context.ChoiceContext, count, context.Card.Owner));
    }

    private static async Task AttackAsync(ComponentRuntimeContext context, decimal amount, int hits = 1)
    {
        if (amount > 0 && hits > 0 && context.Target is { IsAlive: true } target)
        {
            await DamageCmd.Attack(amount).WithHitCount(hits).FromCard(context.Card, context.CardPlay)
                .CGSWithVfx("vfx/vfx_attack_slash").Targeting(target).Execute(context.ChoiceContext);
        }
    }

    private static async Task AttackAllAsync(ComponentRuntimeContext context, decimal amount)
    {
        if (amount > 0 && context.Card.CombatState is { } combat)
        {
            await DamageCmd.Attack(amount).FromCard(context.Card, context.CardPlay)
                .CGSWithVfx("vfx/vfx_attack_slash").TargetingAllOpponents(combat).Execute(context.ChoiceContext);
        }
    }

    private static IEnumerable<IHoverTip> AdditionalHoverTips(ComponentPresentationContext context)
    {
        if (context.RuntimeSpec.SourceZone == RestZone)
            yield return ModKeywordRegistry.CreateHoverTip(CGSKeywords.RestPile);

        IEnumerable<IHoverTip> tips = context.RuntimeSpec.Variant switch
        {
            Domain => [HoverTipFactory.FromPower<CGSDomainPower>()],
            "add_weight" => [HoverTipFactory.FromCard<CGSWeightGain>()],
            "add_dazed" => [HoverTipFactory.FromCard<Dazed>()],
            "fill_with_balls" => [HoverTipFactory.FromCard<CGSBall>()],
            "make_energy_drinks" => [HoverTipFactory.FromCard<CGSEnergyDrink>()],
            "enchant_reserve_step" => HoverTipFactory.FromEnchantment<CGSReserveStepEnchantment>(),
            "block_positive_power" => [HoverTipFactory.FromPower<CGSNoPositivePower>()],
            "enemy_hp_loss_cap" => [HoverTipFactory.FromPower<CGSEnemyAttackLossCapPower>()],
            "damage_and_draw_against_debuff" =>
                [HoverTipFactory.FromPower<VulnerablePower>(), HoverTipFactory.FromPower<WeakPower>()],
            _ => []
        };
        foreach (var tip in tips)
            yield return tip;
    }
}
