using System.Text;
using ChaosCardGenerator;
using ChevalGrandSlay.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using static ChaosCardGenerator.GeneratedCardType;
using static ChaosCardGenerator.GeneratedRarity;

namespace ChevalGrandSlay.Compatibility.AutoAnthony;

internal static partial class ChevalComponents
{
    internal const string ChainDiscount = "chain_discount";
    internal const string RetainedDamage = "retained_damage";
    internal const string FinalKick = "final_kick";
    internal const string Domain = "enter_domain";
    internal const int FinalKickDamage = 20171126;

    internal static bool HasEffect(GeneratedCard card, string variant) => card.Operations.Any(operation =>
        operation.RuntimeSpec is { Opcode: RuntimeOpcode } spec && spec.Variant == variant);

    private sealed record AdditionalCatalog(
        IReadOnlyList<IroncladCardRecipe> Recipes,
        IReadOnlyList<CustomEffect> Effects,
        IReadOnlyList<ComponentValuationRegistration> Valuations);

    private sealed record LinearValue(int Flat, int PerUnit) : IComponentValuation
    {
        public int Estimate(ComponentValuationContext context) =>
            Math.Max(1, Flat + PerUnit * context.Value("amount"));
    }

    private static AdditionalCatalog BuildAdditionalRecipes()
    {
        var effects = new List<CustomEffect>();
        var valuations = new List<ComponentValuationRegistration>();

        ComponentAtom Effect(string id, int amount, string zh, string en, int flat = 0, int perUnit = 0,
            bool rest = false, bool power = false, bool choice = false, string? damageTarget = null,
            string? target = null, bool block = false, int downside = 0, RuntimeValueSlot[]? extra = null)
        {
            var effect = Custom(id, amount, zh, en, rest, power, fixedAmount: perUnit == 0, extra,
                damageTarget, choice, downside > 0, target, block);
            effects.Add(effect);
            valuations.Add(new(Opcode, id, new LinearValue(flat, perUnit), NegativeLinearValuePerUnit: downside));
            return effect.Atom;
        }

        ComponentAtom seekAndDiscount = Effect("seek_and_discount", 1,
            "从抽牌堆或弃牌堆选择 [[amount]] 张牌加入手牌。本回合这些牌的费用各减少1。",
            "Choose [[amount]] cards from your draw or discard pile and add them to your hand. They cost 1 less this turn.",
            perUnit: 425, choice: true);
        ComponentAtom spendAll = Effect("play_remaining_uses", 1,
            "选择手中另一张还有体力的牌，按其剩余体力次数免费打出。",
            "Choose another card in your hand with Stamina remaining. Play it for free once per remaining Stamina.",
            flat: 2200, choice: true);
        ComponentAtom discountThisTurn = Effect("discount_stamina_this_turn", 1,
            "你可以选择手中另一张体力牌，使其本回合费用减少 [[amount]]。",
            "You may choose another Stamina card in your hand. It costs [[amount]] less this turn.",
            perUnit: 275, choice: true);
        ComponentAtom addWeight = Effect("add_weight", 1,
            "将 [[amount]] 张发胖！加入手牌。", "Add [[amount]] Weight Gain! to your hand.", downside: 325);
        ComponentAtom resist = Effect("resist_weak_frail", 2,
            "每当你获得虚弱或脆弱时，该次获得的层数减少 [[amount]]，最低为0。",
            "Whenever you gain Weak or Frail, reduce the amount gained by [[amount]], to a minimum of 0.",
            flat: 250, perUnit: 350, power: true);
        ComponentAtom chainDiscount = Effect(ChainDiscount, 1,
            "本回合打出至少2张攻击牌后，此牌本回合的费用减少1。",
            "After you play at least 2 Attacks this turn, this card costs 1 less for the rest of the turn.", flat: 350);
        ComponentAtom chooseDrawPile = Effect("choose_draw_pile", 1,
            "选择从抽牌堆、弃牌堆或休整牌堆抽 [[amount]] 张牌。",
            "Choose to draw [[amount]] cards from your draw pile, discard pile, or Rest Pile.",
            perUnit: 425, rest: true, choice: true);
        ComponentAtom scryToBottom = Effect("inspect_send_to_bottom", 4,
            "查看抽牌堆顶 [[amount]] 张牌。将其中2张移至牌堆底。升级后可少选。",
            "Look at the top [[amount]] cards of your draw pile. Move 2 to the bottom. Upgrading makes the selection optional.",
            flat: 150, perUnit: 60, choice: true);
        ComponentAtom energyAfterRest = Effect("energy_if_entered_rest", 2,
            "若本回合有牌进入过休整牌堆，获得 [[amount]] 点能量。",
            "If a card entered your Rest Pile this turn, gain [[amount]] Energy.", perUnit: 375, rest: true);
        ComponentAtom drawAfterRest = Effect("draw_if_entered_rest", 1,
            "若本回合有牌进入过休整牌堆，抽 [[amount]] 张牌。",
            "If a card entered your Rest Pile this turn, draw [[amount]] cards.", perUnit: 225, rest: true);
        ComponentAtom stun = Effect("stun_target", 1,
            "击晕该敌人。", "Stun that enemy.", flat: 950, target: "selected_enemy");
        ComponentAtom unpoweredAllDamage = Effect("unpowered_all_damage", 8,
            "对所有敌人造成 [[amount]] 点伤害。这次伤害不受攻击伤害修正。",
            "Deal [[amount]] unpowered damage to ALL enemies.", perUnit: 150, damageTarget: "all_enemies");
        ComponentAtom domain = Effect(Domain, 2,
            "进入领域，持续 [[amount]] 个你的回合，计入本回合。",
            "Enter the Domain for [[amount]] of your turns, including this turn.", perUnit: 900);
        ComponentAtom recoverHalf = Effect("rest_half_to_draw", 1,
            "从休整牌堆选 [[amount]] 张体力牌。此牌为X费时改为X张。不足则全选。恢复每张牌一半的体力上限，向上取整，再移入抽牌堆。",
            "Choose [[amount]] Stamina cards from the Rest Pile, or X if this card costs X. Restore half of each card's maximum Stamina, rounded up, then move them to the draw pile.",
            perUnit: 420, rest: true, choice: true);
        ComponentAtom overflow = Effect("overflow_on_kill", 1,
            "每当你击杀敌人，对其余敌人造成等同于溢出伤害的伤害。",
            "When you kill an enemy, deal damage equal to the overkill to the remaining enemies.", flat: 1700, power: true);
        ComponentAtom recoverEveryone = Effect("rest_all_full_to_draw", 1,
            "回满休整牌堆中所有体力牌的体力，并将休整牌堆中的牌移入抽牌堆。",
            "Fully restore all Stamina in your Rest Pile. Move its cards to your draw pile.", flat: 1625, rest: true);
        ComponentAtom restoreAndReturn = Effect("rest_to_hand_discount", 4,
            "从休整牌堆选择至多 [[amount]] 张体力牌，回满体力并加入手牌。本回合这些牌的费用各减少1。",
            "Choose up to [[amount]] Stamina cards from the Rest Pile. Fully restore them and put them in your hand. They cost 1 less this turn.",
            perUnit: 480, rest: true, choice: true);
        ComponentAtom fillGap = Effect("recover_first_leaving_rest", 1,
            "每回合首次从休整牌堆移出体力牌时，为它恢复1点体力。升级后还获得1点能量。",
            "The first time a Stamina card leaves the Rest Pile each turn, restore 1 Stamina to it. When upgraded, also gain 1 Energy.",
            flat: 1200, rest: true, power: true);
        ComponentAtom finalKick = Effect(FinalKick, 1,
            "只能在能量为0、抽牌堆及弃牌堆均无体力牌且休整牌堆非空时打出。对所有敌人造成20171126点伤害。",
            "Playable only with 0 Energy, a nonempty Rest Pile, and no Stamina cards in the draw or discard pile. Deal 20171126 damage to ALL enemies.",
            flat: 2100, rest: true, damageTarget: "all_enemies");
        ComponentAtom restAreaDamage = Effect("area_damage_on_rest", 16,
            "你的回合中，每有一张牌进入休整牌堆，对所有敌人造成 [[amount]] 点伤害。",
            "During your turn, whenever a card enters the Rest Pile, deal [[amount]] damage to ALL enemies.",
            perUnit: 155, rest: true, power: true);
        ComponentAtom drawAfterFourth = Effect("draw_after_fourth_attack", 1,
            "每个你的回合中，从第4张攻击牌起，每打出一张攻击牌后抽 [[amount]] 张牌。",
            "Each turn, after you play an Attack, draw [[amount]] cards if it was your fourth Attack or later.",
            flat: 375, perUnit: 450, power: true);
        ComponentAtom thirdAttackReward = Effect("third_attack_reward", 1,
            "每回合打出第3张攻击牌后，获得1点能量，对所有敌人造成18点伤害。",
            "After you play your third Attack each turn, gain 1 Energy and deal 18 damage to ALL enemies.",
            flat: 1800, power: true);
        ComponentAtom fillWithBalls = Effect("fill_with_balls", 1,
            "用球填满手牌。", "Fill your hand with Balls.", flat: 1350);
        ComponentAtom retainedDamage = Effect(RetainedDamage, 12,
            "造成 [[amount]] 点伤害。若此牌上回合结束时被保留，额外造成 [[bonus]] 点伤害。",
            "Deal [[amount]] damage. If this card was retained last turn, deal [[bonus]] additional damage.",
            flat: 250, perUnit: 100, damageTarget: "selected_enemy",
            extra: [new("bonus", 4, Upgradable: false)]);
        ComponentAtom hpLossCap = Effect("enemy_hp_loss_cap", 12,
            "敌方回合中，因敌方攻击损失的生命合计至多为 [[amount]]。",
            "During the enemy turn, lose at most [[amount]] total HP from enemy attacks.", flat: 1700);
        ComponentAtom counterFullBlock = Effect("counter_full_block", 9,
            "敌方回合中，每次完全格挡一段攻击伤害时，对所有敌人造成 [[amount]] 点伤害。",
            "During the enemy turn, whenever you fully block a hit, deal [[amount]] damage to ALL enemies.",
            perUnit: 105);
        ComponentAtom nextAttackDamage = Effect("rest_boost_next_attack", 10,
            "每有一张牌进入休整牌堆，下一张攻击牌每段伤害增加 [[amount]]。",
            "Whenever a card enters the Rest Pile, your next Attack deals [[amount]] extra damage per hit.",
            perUnit: 165, rest: true, power: true);
        ComponentAtom blockOnRest = Effect("block_on_rest", 12,
            "你的回合中，每有一张牌进入休整牌堆，获得 [[amount]] 点格挡。",
            "During your turn, whenever a card enters the Rest Pile, gain [[amount]] Block.",
            perUnit: 135, rest: true, power: true);
        ComponentAtom restHitCount = Effect("hits_per_resting_card", 8,
            "休整牌堆每有一张牌，对一名敌人造成 [[amount]] 点伤害一次。",
            "Deal [[amount]] damage to an enemy once per card in your Rest Pile.",
            perUnit: 200, rest: true, damageTarget: "selected_enemy");
        ComponentAtom addDazed = Effect("add_dazed", 2,
            "将 [[amount]] 张晕眩随机洗入抽牌堆。", "Shuffle [[amount]] Dazed into your draw pile.", downside: 200);
        ComponentAtom extraTurn = Effect("extra_turn", 1,
            "回合结束后获得一个额外回合，其开始时获得1点力量及1点敏捷。额外回合中无法再获得额外回合。",
            "Take an extra turn after this one. At its start, gain 1 Strength and 1 Dexterity. You cannot gain another extra turn during it.",
            flat: 2850);
        ComponentAtom doubleFirstHit = Effect("play_attack_double_first_hit", 1,
            "选择手中一张至少剩余2点体力的攻击牌，对该敌人免费打出。该牌首次攻击额外命中一次。",
            "Choose an Attack in hand with at least 2 Stamina. Play it for free against that enemy. Its first attack hits an extra time.",
            flat: 1800, choice: true, target: "selected_enemy");
        ComponentAtom perfectDefend = Effect("perfect_defend", 16,
            "每个你的回合开始时获得 [[amount]] 点格挡。每次完全格挡一段攻击时，获得1点能量并抽1张牌。",
            "At the start of your turn, gain [[amount]] Block. Whenever you fully block a hit, gain 1 Energy and draw 1 card.",
            flat: 1100, perUnit: 90, power: true);
        ComponentAtom upgradeDrawCards = Effect("upgrade_draw_cards", 2,
            "从抽牌堆选择至多 [[amount]] 张牌，在本场战斗中升级。",
            "Choose up to [[amount]] cards in your draw pile. Upgrade them for this combat.", perUnit: 475, choice: true);
        ComponentAtom blockNextTurn = Effect("block_next_turn", 16,
            "下回合获得 [[amount]] 点格挡。", "Gain [[amount]] Block next turn.", perUnit: 75, block: true);
        ComponentAtom threeHits = Effect("three_hits", 10,
            "造成 [[amount]] 点伤害3次。", "Deal [[amount]] damage 3 times.", perUnit: 300, damageTarget: "selected_enemy");
        ComponentAtom cleanseDamage = Effect("cleanse_damage", 10,
            "移除自身至多 [[limit]] 层异常状态。此牌为X费时改为X层。每实际移除一层，对所有敌人造成 [[amount]] 点伤害。",
            "Remove up to [[limit]] stacks of debuffs from yourself, or X if this card costs X. Deal [[amount]] damage to ALL enemies per stack removed.",
            flat: 250, perUnit: 125, damageTarget: "all_enemies", extra: [new("limit", 2, Upgradable: false)]);
        ComponentAtom restRandomDamage = Effect("random_damage_on_rest", 14,
            "你的回合中，每有一张牌进入休整牌堆，对一名随机敌人造成 [[amount]] 点伤害。",
            "During your turn, whenever a card enters the Rest Pile, deal [[amount]] damage to a random enemy.",
            perUnit: 125, rest: true, power: true);
        ComponentAtom restoreAndEmpower = Effect("empower_resting_attack", 16,
            "选择一张休整中的体力攻击牌，回满体力并加入手牌。下次打出时每段攻击伤害增加 [[amount]]。",
            "Choose a resting Stamina Attack. Fully restore it and put it in your hand. Its next play deals [[amount]] extra damage per hit.",
            flat: 450, perUnit: 145, rest: true, choice: true);
        ComponentAtom oldPact = Effect("energy_strength_each_turn", 1,
            "你的回合开始时，获得1点能量和4点力量。升级后还抽1张牌。",
            "At the start of your turn, gain 1 Energy and 4 Strength. When upgraded, also draw 1 card.",
            flat: 2400, power: true);
        ComponentAtom recycleDiscard = Effect("shuffle_selected_discard", 3,
            "从弃牌堆选择至多 [[amount]] 张牌，随机插入抽牌堆。",
            "Choose up to [[amount]] cards in your discard pile. Shuffle them into your draw pile.",
            perUnit: 175, choice: true);
        ComponentAtom takeEnemyBlock = Effect("take_enemy_block", 20,
            "移除敌人至多 [[amount]] 点格挡并获得等量格挡。若敌人没有格挡，扣除其12点生命。",
            "Remove up to [[amount]] Block from an enemy and gain that much Block. If it has no Block, it loses 12 HP.",
            flat: 675, perUnit: 75, damageTarget: "selected_enemy", block: true);
        ComponentAtom shuffle = Effect("shuffle_draw_and_discard", 1,
            "将抽牌堆和弃牌堆一起洗成新的抽牌堆。",
            "Shuffle your draw and discard piles together into a new draw pile.", flat: 350);
        ComponentAtom enchantReserve = Effect("enchant_reserve_step", 1,
            "选择手中另一张还有体力的牌，本场战斗为其附加余兴未尽。",
            "Choose another card with Stamina remaining. Enchant it with One More Step for this combat.",
            flat: 675, choice: true);
        ComponentAtom discountThisCombat = Effect("discount_this_combat", 1,
            "选择一张手牌，本场战斗的费用降低 [[amount]]。",
            "Choose a card in your hand. It costs [[amount]] less for this combat.", perUnit: 600, choice: true);
        ComponentAtom retainHandCard = Effect("retain_hand_card", 1,
            "选择手中一张牌，使其本回合结束时保留。",
            "Choose a card in hand to Retain at the end of this turn.", flat: 250, choice: true);
        ComponentAtom replayRestingCards = Effect("play_restored_cards", 4,
            "选择至多 [[amount]] 张休整中的体力牌。回满体力后免费打出，再回满并移入弃牌堆。",
            "Choose up to [[amount]] resting Stamina cards. Fully restore and play them for free, then restore them again and put them in the discard pile.",
            perUnit: 700, rest: true, choice: true);
        ComponentAtom drawWithNoStamina = Effect("draw_when_no_stamina_in_hand", 2,
            "若手中没有其他仍有体力的牌，抽 [[amount]] 张牌。",
            "If there are no other cards in hand with Stamina remaining, draw [[amount]] cards.", perUnit: 225);
        ComponentAtom takeTopCards = Effect("select_among_top_cards", 1,
            "查看抽牌堆顶 [[look]] 张牌，选择其中 [[amount]] 张加入手牌。",
            "Look at the top [[look]] cards of your draw pile. Put [[amount]] of them into your hand.",
            flat: 150, perUnit: 325, choice: true, extra: [new("look", 4, Upgradable: false)]);
        ComponentAtom makeDrinks = Effect("make_energy_drinks", 2,
            "将 [[amount]] 张超大份汉堡肉加入手牌。", "Add [[amount]] Extra-Large Burger Patties to your hand.",
            perUnit: 480);
        ComponentAtom doubleBlock = Effect("double_current_block", 1,
            "将当前格挡翻倍。", "Double your current Block.", flat: 900, block: true);
        ComponentAtom damageFromBlock = Effect("damage_equal_to_block", 1,
            "对所有敌人造成等于你当前格挡值的伤害。",
            "Deal damage equal to your current Block to ALL enemies.", flat: 1250, damageTarget: "all_enemies");
        ComponentAtom noPositive = Effect("block_positive_power", 1,
            "给予该敌人你在看别的孩子吗？",
            "Apply Are You Looking at Another Girl? to that enemy.", flat: 700, target: "selected_enemy");
        ComponentAtom damageDrawAgainstDebuff = Effect("damage_and_draw_against_debuff", 12,
            "造成 [[amount]] 点伤害。若攻击前目标已经有易伤或虚弱，抽 [[draw]] 张牌。",
            "Deal [[amount]] damage. If the target had Vulnerable or Weak before the hit, draw [[draw]] cards.",
            flat: 430, perUnit: 100, damageTarget: "selected_enemy", extra: [new("draw", 2, Upgradable: false)]);
        ComponentAtom spendStaminaDraw = Effect("spend_other_stamina_to_draw", 2,
            "选择一张来自牌组且至少剩余2点体力的手牌。消耗其2点体力，然后抽 [[amount]] 张牌。",
            "Choose a card in hand from your deck with at least 2 Stamina. Spend 2 of its Stamina, then draw [[amount]] cards.",
            perUnit: 225, choice: true);
        ComponentAtom drawOnLeaveRest = Effect("draw_on_leave_rest", 1,
            "你的回合中，每有一张牌移出休整牌堆，抽1张牌。升级后还获得10点格挡。",
            "During your turn, whenever a card leaves the Rest Pile, draw 1 card. When upgraded, also gain 10 Block.",
            flat: 1000, rest: true, power: true);
        ComponentAtom blockOnCostlyAttack = Effect("block_on_costly_attack", 18,
            "你的回合中，每打出一张基础费用至少为1的攻击牌，获得 [[amount]] 点格挡。",
            "During your turn, after you play an Attack with a base cost of at least 1, gain [[amount]] Block.",
            perUnit: 125, power: true);
        ComponentAtom drawAndDiscardNew = Effect("draw_and_discard_drawn", 2,
            "抽 [[amount]] 张牌，然后从本次抽到的牌中丢弃一张。",
            "Draw [[amount]] cards, then discard one of those cards.", perUnit: 225, choice: true);
        ComponentAtom blockOnSkill = Effect("block_on_skill", 10,
            "你的回合中，每打出一张技能牌后，获得 [[amount]] 点格挡。",
            "During your turn, after you play a Skill, gain [[amount]] Block.", perUnit: 145, power: true);

        return new([
            Recipe<CGSAimForTheFinish>("心中的终点", "The Finish in My Heart", 0, Skill, Uncommon, [seekAndDiscount]),
            Recipe<CGSAllOut>("我也想被记住", "I Want to Be Remembered, Too", 3, Skill, Uncommon, [spendAll]),
            Recipe<CGSAroundTheBend>("转角后的蓝", "Blue Beyond the Bend", 1, Skill, Common, [Draw(1), discountThisTurn]),
            Recipe<CGSBalanceTheWeight>("多吃一口", "One More Bite", 0, Skill, Uncommon, [Energy(2), addWeight]),
            Recipe<CGSBorrowForceEscape>("不惧不安", "Unfazed by Unease", 2, Power, Rare, [resist]),
            Recipe<CGSBreakTheOpening>("向前探身", "Leaning Forward", 1, Attack, Uncommon, [Vulnerable(2), Damage(10)]),
            Recipe<CGSChainStep>("脚步渐轻", "Lighter Steps", 1, Attack, Common, [Damage(10), chainDiscount]),
            Recipe<CGSChaseTheWind>("衣角逐风", "A Windblown Hem", 1, Attack, Uncommon, [Damage(10), chooseDrawPile]),
            Recipe<CGSCorrectTheFormation>("整理鞋带", "Tying My Shoelaces", 0, Skill, Uncommon, [scryToBottom]),
            Recipe<CGSCountTheCost>("汗水会回应", "Effort Rewarded", 0, Skill, Uncommon, [Draw(2), energyAfterRest]),
            Recipe<CGSCushionLanding>("小歇", "A Short Rest", 1, Skill, Common, [Block(8), drawAfterRest]),
            Recipe<CGSCutOffRetreat>("不再躲藏", "No More Hiding", 1, Attack, Uncommon, [Damage(12), stun]),
            Recipe<CGSDisruptFootwork>("别小看我", "Don't Underestimate Me", 0, Attack, Uncommon, [VulnerableAll(2), unpoweredAllDamage]),
            Recipe<CGSDomain>("我也能耀眼", "I Can Shine, Too", 0, Skill, Rare, [domain]),
            Recipe<CGSDrawFinishLine>("今天的终点", "Today's Finish Line", -1, Skill, Uncommon, [recoverHalf, Draw(1)]),
            Recipe<CGSEarthshakingStep>("小声的震撼", "A Quiet Shock", 3, Attack, Rare, [DamageAll(28)]),
            Recipe<CGSEchoOfTheFinish>("打向远方!", "Hit It Far!", 2, Power, Rare, [overflow]),
            Recipe<CGSFateEcho>("潮声再起", "The Tide Calls Again", 2, Skill, Uncommon, [recoverEveryone]),
            Recipe<CGSFateGuidance>("Tidebreaker", "Tidebreaker", 1, Skill, Rare, [restoreAndReturn]),
            Recipe<CGSFillTheGap>("小小的关照", "A Little Care", 1, Power, Rare, [fillGap]),
            Recipe<CGSFinalKick>("末脚", "Final Kick", 0, Attack, Rare, [finalKick]),
            Recipe<CGSFinishLineEcho>("光的残响", "Echoes of Light", 2, Power, Rare, [restAreaDamage]),
            Recipe<CGSFlowingAssault>("顺风的航程", "A Voyage with the Wind", 2, Power, Uncommon, [drawAfterFourth]),
            Recipe<CGSForgeAhead>("进攻姿态", "Offensive Stance", 2, Power, Rare, [thirdAttackReward]),
            Recipe<CGSHandoverMoment>("轮到你了", "Your Turn", 0, Skill, Common, [Draw(2), Energy(2)]),
            Recipe<CGSLendReserve>("传球", "Pass the Ball", 1, Skill, Uncommon, [fillWithBalls]),
            Recipe<CGSLightStep>("怯怯的一步", "A Timid Step", 0, Attack, Common, [Damage(10)]),
            Recipe<CGSLittleBravePlus>("自信大方", "Poised and Confident", 2, Attack, Ancient, [Damage(36), Block(24)]),
            Recipe<CGSLongSprint>("迟来的疾驰", "A Belated Burst", 2, Attack, Uncommon, [retainedDamage]),
            Recipe<CGSMeasuredAdvanceAndRetreat>("稳守本垒", "Guarding Home Plate", 2, Skill, Rare, [hpLossCap]),
            Recipe<CGSNoConcession>("好胜的内心", "A Competitive Heart", 1, Skill, Uncommon, [Block(6), counterFullBlock]),
            Recipe<CGSNoRegretsEnding>("没跑完的话", "An Unfinished Run", 2, Power, Rare, [nextAttackDamage]),
            Recipe<CGSOldRoadWarmth>("点滴积累", "Little by Little", 1, Power, Uncommon, [blockOnRest]),
            Recipe<CGSOldTraceReturnsEdge>("汗水的痕迹", "Traces of Effort", 1, Attack, Uncommon, [restHitCount]),
            Recipe<CGSOldWallNewEdge>("放手一搏", "Take the Leap", 2, Attack, Uncommon, [addDazed, DamageTwice(12)]),
            Recipe<CGSOneBeatAhead>("憧憬的前方", "Beyond Admiration", 3, Skill, Ancient, [extraTurn]),
            Recipe<CGSPourIntoOneMove>("全力投球", "An All-Out Pitch", 1, Skill, Rare, [doubleFirstHit]),
            Recipe<CGSPrefectDefend>("自制心", "Self-Control", 2, Power, Rare, [perfectDefend]),
            Recipe<CGSPrepareEarly>("独自晨练", "Morning Practice Alone", 0, Skill, Common, [upgradeDrawCards]),
            Recipe<CGSPrepareForNextStep>("明天仍然赴约", "Same Time Tomorrow", 2, Skill, Uncommon, [Block(14), blockNextTurn]),
            Recipe<CGSRapidCombination>("三连挥棒", "Three Swings in a Row", 1, Attack, Uncommon, [threeHits]),
            Recipe<CGSRegainTheEdge>("说我也可以", "Tell Me I Can, Too", -1, Attack, Uncommon, [cleanseDamage]),
            Recipe<CGSRemainingUsesSense>("疲惫的倔强", "Stubborn Through Fatigue", 2, Power, Uncommon, [restRandomDamage]),
            Recipe<CGSRenewTheContest>("再较量一次", "Another Race Together", 1, Skill, Rare, [restoreAndEmpower]),
            Recipe<CGSRenewTheOldPact>("那时约定的", "What We Promised", 2, Power, Rare, [oldPact]),
            Recipe<CGSReorganizeThePace>("听一会海风", "Listen to the Sea Breeze", 1, Skill, Common, [Block(20)]),
            Recipe<CGSReplanTheRoute>("宏伟旅程", "A Grand Journey", 1, Skill, Uncommon, [recycleDiscard, Draw(1)]),
            Recipe<CGSRepurposeBulwark>("借来的从容", "Borrowed Composure", 1, Skill, Uncommon, [takeEnemyBlock]),
            Recipe<CGSReschedule>("重新出发", "Setting Out Again", 0, Skill, Uncommon, [shuffle, Draw(3)]),
            Recipe<CGSReserveAStep>("还能再钓一条", "One More Catch", 1, Skill, Common, [Block(5), enchantReserve]),
            Recipe<CGSReturnMarker>("回垒的讯号", "A Signal to Head Home", 0, Skill, Uncommon, [discountThisCombat, Draw(1)]),
            Recipe<CGSReviewThePace>("我的宝藏", "My Treasure", 1, Skill, Common, [Draw(2), retainHandCard]),
            Recipe<CGSRewriteFate>("青空下的约定", "Promise under the Sky", 2, Skill, Rare, [replayRestingCards]),
            Recipe<CGSSidewaysEvasion>("躲开人群", "Away from the Crowd", 1, Skill, Common, [Block(10), drawWithNoStamina]),
            Recipe<CGSSmoothBreathing>("独处的节奏", "The Rhythm of Solitude", 0, Skill, Uncommon, [takeTopCards]),
            Recipe<CGSSportsDrinkSupply>("能量补给", "Energy Replenishment", 1, Skill, Uncommon, [makeDrinks]),
            Recipe<CGSSpreadTheOpening>("浪纹扩散", "Spreading Ripples", 0, Skill, Uncommon, [VulnerableAll(2)]),
            Recipe<CGSSteadyPace>("慢慢站稳", "Finding My Footing", 1, Skill, Uncommon, [doubleBlock]),
            Recipe<CGSStepSteady>("坚定步伐", "Steady Footsteps", 1, Attack, Common, [damageFromBlock]),
            Recipe<CGSSuppressTheirSpirit>("请记住我", "Please Remember Me", 1, Attack, Uncommon, [Damage(8), noPositive]),
            Recipe<CGSTearOpenDefense>("请看着我", "Please Look at Me", 1, Attack, Uncommon, [damageDrawAgainstDebuff]),
            Recipe<CGSTurnTheOldPage>("昨日的练习", "Yesterday's Practice", 0, Skill, Uncommon, [spendStaminaDraw]),
            Recipe<CGSTurnTogether>("以后...再一起跑吧", "Someday... Let's Run Together Again", 1, Power, Uncommon, [drawOnLeaveRest]),
            Recipe<CGSUnwaveringStep>("即使有些笨拙", "Even If I'm Clumsy", 1, Power, Rare, [blockOnCostlyAttack]),
            Recipe<CGSWaitAndSee>("静候咬钩", "Waiting for a Bite", 1, Skill, Uncommon, [Block(12), drawAndDiscardNew]),
            Recipe<CGSWinningRhythm>("身手敏捷", "Nimble", 2, Power, Rare, [blockOnSkill]),
            Recipe<CGSWinningShift>("碧海航线", "Azure Sea Route", 0, Skill, Rare, [Energy(4), Draw(3), addDazed]),
        ], effects, valuations);
    }

    private static CustomEffect ClearDistractionsEffect() => Custom("exhaust_status_draw", 1,
        "选择并消耗手中一张状态牌或诅咒牌，然后抽 [[amount]] 张牌。",
        "Exhaust a Status or Curse from your hand, then draw [[amount]] cards.", requiresChoice: true);

    private static ComponentAtom Damage(int amount) => Native("ironclad/anger", 0, "damage", amount);
    private static ComponentAtom DamageTwice(int amount) => Native("ironclad/twin_strike", 0, "damage", amount);
    private static ComponentAtom DamageAll(int amount) => Native("ironclad/thunderclap", 0, "damage", amount);
    private static ComponentAtom Block(int amount) => Native("ironclad/defend_ironclad", 0, "block", amount);
    private static ComponentAtom Draw(int count) => Native("ironclad/shrug_it_off", 1, "draw", count);
    private static ComponentAtom Energy(int amount) => Native("ironclad/bloodletting", 1, "energy", amount);
    private static ComponentAtom Vulnerable(int amount) => Native("ironclad/bash", 1, "amount", amount);
    private static ComponentAtom VulnerableAll(int amount) => Native("ironclad/thunderclap", 1, "amount", amount);

    private static void RegisterAdditionalNames(IReadOnlyList<IroncladCardRecipe> recipes)
    {
        CardNameGenerator.RegisterExternalParts(ProfileId + ":additional-names",
            recipes.Select(recipe =>
            {
                var english = recipe.EnglishTitle.Split(' ');
                int middle = english.Length / 2;
                string prefix = middle > 0 ? string.Join(' ', english[..middle]) + " " : "";
                string suffix = middle + 1 < english.Length ? " " + string.Join(' ', english[(middle + 1)..]) : "";
                var chinese = recipe.ChineseTitle.EnumerateRunes().Chunk(2)
                    .Select(chunk => string.Concat(chunk.Select(rune => rune.ToString()))).ToArray();
                return new ComponentCardNameParts(recipe.Id, chinese, prefix, english[middle], suffix);
            }));
    }

    private static void VerifyCoverage(IReadOnlyList<IroncladCardRecipe> recipes)
    {
        // Tokens retain their native models. They are either produced by a component or used as pile choices.
        var nativeOnly = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(CGSEmptyShell), nameof(CGSWeightGain), nameof(CGSBall), nameof(CGSEnergyDrink),
            nameof(CGSChaseTheWindDrawPile), nameof(CGSChaseTheWindDiscardPile), nameof(CGSChaseTheWindRestPile)
        };
        var registered = typeof(Entry).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(CardModel).IsAssignableFrom(type) &&
                type.IsDefined(typeof(RegisterCardAttribute), inherit: false))
            .Select(type => type.Name).ToHashSet(StringComparer.Ordinal);
        var covered = recipes.Select(recipe => recipe.Id).ToHashSet(StringComparer.Ordinal);

        if (covered.Count != recipes.Count || covered.Overlaps(nativeOnly))
            throw new InvalidOperationException("CGS AutoAnthony recipes have duplicate or token card IDs.");

        covered.UnionWith(nativeOnly);
        if (!registered.SetEquals(covered))
            Entry.Logger.Warn("CGS AutoAnthony coverage mismatch. Missing: " +
                string.Join(", ", registered.Except(covered).Order()) + ". Unknown: " +
                string.Join(", ", covered.Except(registered).Order()) + ".");
    }
}
