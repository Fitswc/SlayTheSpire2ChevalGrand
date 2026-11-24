using global::AutoAnthony;
using ChevalGrandSlay.Cards;
using ChevalGrandSlay.Enchantments;
using ChevalGrandSlay.Mechanics;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Combat.HandSize;

namespace ChevalGrandSlay.Compatibility.AutoAnthony;

internal static partial class ChevalComponents
{
    private static async Task<bool> ExecuteCardMovementAsync(ComponentRuntimeContext context, int amount)
    {
        var source = context.Card;
        var player = source.Owner;
        var owner = player.Creature;
        var choice = context.ChoiceContext;
        var hand = PileType.Hand.GetPile(player);
        var draw = PileType.Draw.GetPile(player);
        var discard = PileType.Discard.GetPile(player);
        var rest = Entry.CGSFatePile.GetPile(player);
        if (CombatManager.Instance.IsOverOrEnding)
            return true;

        switch (context.RuntimeSpec.Variant)
        {
            case "seek_and_discount":
                var seek = await ChooseCards(context, draw.Cards.Concat(discard.Cards),
                    ModelDb.Card<CGSAimForTheFinish>().SelectionScreenPrompt, Math.Min(amount, FreeHandSlots(source)));
                foreach (var card in seek)
                    if ((await CardPileCmd.Add(card, hand)).success && card.Pile == hand)
                        card.EnergyCost.AddThisTurn(-1);
                return true;
            case "play_remaining_uses":
                if (await ChooseHand<CGSAllOut>(context, card => CanAutoPlay(card) && CGSStamina.HasAtLeast(card, 1))
                    is { } repeated)
                {
                    int plays = CGSStamina.GetRemaining(repeated);
                    for (int i = 0;
                         i < plays && CGSStamina.HasAtLeast(repeated, 1) &&
                         !CombatManager.Instance.IsOverOrEnding;
                         i++)
                        await CardCmd.AutoPlay(choice, repeated, null);
                }
                return true;
            case "discount_stamina_this_turn":
                var discountPrompt = ModelDb.Card<CGSAroundTheBend>().SelectionScreenPrompt;
                discountPrompt.Add("energyPrefix", EnergyIconHelper.GetPrefix(source));
                var discounted = await CardSelectCmd.FromHand(choice, player,
                    new CardSelectorPrefs(discountPrompt, 0, 1),
                    card => card != source && CGSStamina.HasStamina(card) && !card.EnergyCost.CostsX, source);
                discounted.FirstOrDefault()?.EnergyCost.AddThisTurn(-amount);
                return true;
            case "add_weight":
                await GenerateCards<CGSWeightGain>(context, amount, PileType.Hand);
                return true;
            case "choose_draw_pile":
                context.ReplaceDrawnCards(await CGSChaseTheWind.DrawFromChosenPile(source, choice, amount));
                return true;
            case "exhaust_status_draw":
                if (await ChooseHand<CGSClearDistractions>(context, card => card.Type is CardType.Status or CardType.Curse)
                    is { } exhausted)
                {
                    await CardCmd.Exhaust(choice, exhausted);
                    await DrawAsync(context, amount);
                }
                return true;
            case "inspect_send_to_bottom":
                var top = draw.Cards.Take(amount).ToArray();
                var bottom = await ChooseCards(context, top, ModelDb.Card<CGSCorrectTheFormation>().SelectionScreenPrompt,
                    2, optional: source.IsUpgraded);
                foreach (var card in top.Where(bottom.Contains))
                    await CardPileCmd.Add(card, PileType.Draw);
                return true;
            case "rest_half_to_draw":
                var recoverHalf = await ChooseCards(context, rest.Cards.Where(CGSStamina.CanRecoverStamina),
                    ModelDb.Card<CGSDrawFinishLine>().SelectionScreenPrompt, SelectionLimit(context, amount));
                foreach (var card in recoverHalf)
                {
                    if (card.Pile != rest)
                        continue;
                    CGSStamina.Restore(card, Math.Ceiling(card.DynamicVars[CGSStamina.MaxUses].BaseValue / 2m));
                    await CardPileCmd.Add(card, PileType.Draw);
                }
                return true;
            case "rest_all_full_to_draw":
                var resting = rest.Cards.Where(card => !CGSStamina.IsLocked(card)).ToArray();
                foreach (var card in resting.Where(CGSStamina.HasStamina))
                    CGSStamina.RestoreFull(card);
                if (resting.Length > 0)
                    await CardPileCmd.Add(resting, PileType.Draw);
                return true;
            case "rest_to_hand_discount":
                var returnCards = await ChooseCards(context, rest.Cards.Where(CGSStamina.CanRecoverStamina),
                    ModelDb.Card<CGSFateGuidance>().SelectionScreenPrompt, Math.Min(amount, FreeHandSlots(source)),
                    optional: true, minimum: 1);
                foreach (var card in returnCards)
                {
                    if (card.Pile != rest)
                        continue;
                    CGSStamina.RestoreFull(card);
                    if ((await CardPileCmd.Add(card, hand)).success && card.Pile == hand)
                        card.EnergyCost.AddThisTurn(-1);
                }
                return true;
            case "fill_with_balls":
                await GenerateCards<CGSBall>(context, FreeHandSlots(source), PileType.Hand);
                return true;
            case "add_dazed":
                await GenerateCards<Dazed>(context, amount, PileType.Draw, CardPilePosition.Random);
                return true;
            case "play_attack_double_first_hit":
                if (context.Target is not { IsAlive: true } target)
                    return true;
                if (await ChooseHand<CGSPourIntoOneMove>(context,
                        card => card.Type == CardType.Attack && CanAutoPlay(card) && CGSStamina.HasAtLeast(card, 2))
                    is { } attack)
                {
                    var power = await Apply<CGSFirstStrikeTwicePower>(context, 1);
                    power?.SetAttack(attack);
                    try
                    {
                        await CardCmd.AutoPlay(choice, attack, target);
                    }
                    finally
                    {
                        if (power is not null)
                            await PowerCmd.Remove(power);
                    }
                }
                return true;
            case "upgrade_draw_cards":
                var upgrades = await ChooseCards(context,
                    draw.Cards.Where(card => card.CurrentUpgradeLevel < card.MaxUpgradeLevel),
                    CardSelectorPrefs.UpgradeSelectionPrompt, amount, optional: true);
                foreach (var card in upgrades)
                    CardCmd.Upgrade(card);
                return true;
            case "empower_resting_attack":
                var empowered = await ChooseCards(context,
                    rest.Cards.Where(card => card.Type == CardType.Attack && CGSStamina.CanRecoverStamina(card)),
                    ModelDb.Card<CGSRenewTheContest>().SelectionScreenPrompt, 1);
                if (empowered.FirstOrDefault() is { } restored && restored.Pile == rest && FreeHandSlots(source) > 0)
                {
                    CGSStamina.RestoreFull(restored);
                    if ((await CardPileCmd.Add(restored, hand)).success && restored.Pile == hand)
                    {
                        var power = owner.GetPower<CGSRenewTheContestPower>() ??
                            await Apply<CGSRenewTheContestPower>(context, amount);
                        power?.BoostNextPlay(restored);
                    }
                }
                return true;
            case "shuffle_selected_discard":
                var recycled = await ChooseCards(context, discard.Cards,
                    ModelDb.Card<CGSReplanTheRoute>().SelectionScreenPrompt, amount, optional: true);
                foreach (var card in recycled)
                    if (card.Pile == discard)
                        await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Random);
                return true;
            case "shuffle_draw_and_discard":
                await CardPileCmd.Shuffle(choice, player);
                return true;
            case "enchant_reserve_step":
                var enchantment = ModelDb.Enchantment<CGSReserveStepEnchantment>();
                if (await ChooseHand<CGSReserveAStep>(context, enchantment.CanEnchant) is { } enchanted &&
                    enchanted.Enchantment is not CGSReserveStepEnchantment)
                    CardCmd.Enchant<CGSReserveStepEnchantment>(enchanted, 1m);
                return true;
            case "discount_this_combat":
                if (await ChooseHand<CGSReturnMarker>(context, card => !card.EnergyCost.CostsX) is { } cheaper)
                    cheaper.EnergyCost.AddThisCombat(-amount);
                return true;
            case "retain_hand_card":
                if (await ChooseHand<CGSReviewThePace>(context) is { } retained)
                    retained.GiveSingleTurnRetain();
                return true;
            case "play_restored_cards":
                var replayed = await ChooseCards(context,
                    rest.Cards.Where(card => CGSStamina.CanRecoverStamina(card) &&
                        card is not ChevalChaosCard { IsResolving: true }),
                    ModelDb.Card<CGSRewriteFate>().SelectionScreenPrompt, amount, optional: true, minimum: 1);
                foreach (var card in replayed)
                {
                    if (CombatManager.Instance.IsOverOrEnding || card.Pile != rest)
                        continue;
                    CGSStamina.RestoreFull(card);
                    await CardCmd.AutoPlay(choice, card, null);
                    CGSStamina.RestoreFull(card);
                    await CardPileCmd.Add(card, PileType.Discard);
                }
                return true;
            case "select_among_top_cards":
                await CardPileCmd.ShuffleIfNecessary(choice, player);
                var chosenTop = await ChooseCards(context, draw.Cards.Take(Value(context, "look")),
                    ModelDb.Card<CGSSmoothBreathing>().SelectionScreenPrompt, Math.Min(amount, FreeHandSlots(source)));
                if (chosenTop.Count > 0)
                    await CardPileCmd.Add(chosenTop, hand);
                return true;
            case "make_energy_drinks":
                await GenerateCards<CGSEnergyDrink>(context, amount, PileType.Hand);
                return true;
            case "spend_other_stamina_to_draw":
                if (await ChooseHand<CGSTurnTheOldPage>(context,
                        card => card.DeckVersion is not null && CGSStamina.HasAtLeast(card, 2)) is { } spent)
                {
                    await CGSStamina.SpendAndRestIfDepleted(spent, 2m);
                    await DrawAsync(context, amount);
                }
                return true;
            case "draw_and_discard_drawn":
                var drawn = (await CardPileCmd.Draw(choice, amount, player)).ToArray();
                context.ReplaceDrawnCards(drawn);
                if (drawn.Length == 0)
                    return true;
                var selected = await CardSelectCmd.FromHandForDiscard(choice, player,
                    new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1), drawn.Contains, source);
                if (selected.FirstOrDefault() is { } discarded)
                    await CardCmd.Discard(choice, discarded);
                return true;
            default:
                return false;
        }
    }

    private static int FreeHandSlots(CardModel source) => Math.Max(0,
        MaxHandSizeCalculator.Calculate(source.Owner) - PileType.Hand.GetPile(source.Owner).Cards.Count);

    private static bool CanAutoPlay(CardModel card) => card is not ChevalChaosCard { IsResolving: true };

    private static async Task<CardModel?> ChooseHand<T>(ComponentRuntimeContext context,
        Func<CardModel, bool>? filter = null) where T : CardModel
    {
        var source = context.Card;
        var selected = await CardSelectCmd.FromHand(context.ChoiceContext, source.Owner,
            new CardSelectorPrefs(ModelDb.Card<T>().SelectionScreenPrompt, 1),
            card => card != source && (filter is null || filter(card)), source);
        return selected.FirstOrDefault();
    }

    private static async Task<IReadOnlyList<CardModel>> ChooseCards(ComponentRuntimeContext context,
        IEnumerable<CardModel> pool, LocString prompt, int maximum, bool optional = false, int minimum = 0)
    {
        var candidates = pool.Where(card => card != context.Card).ToList();
        int count = Math.Min(maximum, candidates.Count);
        if (count <= 0)
            return [];
        return (await CardSelectCmd.FromSimpleGrid(context.ChoiceContext, candidates, context.Card.Owner,
            new CardSelectorPrefs(prompt, optional ? Math.Min(minimum, count) : count, count))).ToArray();
    }

    private static async Task GenerateCards<T>(ComponentRuntimeContext context, int count, PileType pile,
        CardPilePosition position = CardPilePosition.Bottom) where T : CardModel
    {
        var source = context.Card;
        if (count <= 0 || source.CombatState is not { } combat)
            return;
        var cards = Enumerable.Range(0, count).Select(_ => combat.CreateCard<T>(source.Owner)).ToArray();
        var added = await CardPileCmd.AddGeneratedCardsToCombat(cards, pile, source.Owner, position);
        if (pile == PileType.Draw)
            CardCmd.PreviewCardPileAdd(added);
    }
}
