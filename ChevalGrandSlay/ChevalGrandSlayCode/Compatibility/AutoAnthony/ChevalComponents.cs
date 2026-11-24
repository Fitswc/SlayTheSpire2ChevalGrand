using global::AutoAnthony;
using ChaosCardGenerator;
using ChevalGrandSlay.Cards;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using ChevalGrandSlay.Powers;
using ChevalGrandSlay.Utilities;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;
using GeneratorTag = ChaosCardGenerator.CardTag;

namespace ChevalGrandSlay.Compatibility.AutoAnthony;

internal static partial class ChevalComponents
{
    internal const string ProfileId = "chevalgrandslay:autoanthony";
    internal const GeneratedCharacter Archetype = GeneratedCharacter.Ironclad;
    internal static readonly ComponentProfileRequest Request = new(ProfileId, Archetype, false);
    internal static ComponentProfileRequest RequestFor(GeneratedCardType type) =>
        new(ProfileId + ":" + type.ToString().ToLowerInvariant(), Archetype, false);

    internal const string RuntimeOpcode = "cgs_card_effect";
    private const string Opcode = RuntimeOpcode;
    private const string RestZone = "cgs_rest";
    private const string RecoverAll = "recover_all_to_draw";
    private const string RecoverSkills = "recover_skills_to_draw";
    private const string Counter = "counter_attack";
    private const string WideView = "wide_view";
    private const string BorrowFromPast = "borrow_from_past";
    private const string EnergyReserve = "energy_reserve";
    private const string DrawToSize = "draw_to_size";
    private const string Pierce = "pierce";
    private const string FinishSprint = "finish_sprint";
    private const string FinishChain = "finish_chain";
    private const string WinLine = "win_line";
    private const string RestBlock = "rest_block";

    private sealed record CustomEffect(ComponentAtom Atom, ComponentLocalizationRegistration Text);

    internal static void Register()
    {
        var recoverAll = Custom(RecoverAll, 1, "为休整牌堆中每张体力牌恢复 [[amount]] 点体力。将回满的牌移入抽牌堆。",
            "Restore [[amount]] Stamina to each card in the Rest Pile. Move fully restored cards to the draw pile.", rest: true);
        var recoverSkills = Custom(RecoverSkills, 2, "至多选择 [[amount]] 张休整中的技能牌，回满体力并移入抽牌堆。",
            "Choose up to [[amount]] resting Skills. Fully restore their Stamina and move them to the draw pile.", rest: true);
        var counter = Custom(Counter, 6, "获得 [[amount]] 层我也会还手。",
            "Gain [[amount]] I Can Fight Back, Too.");
        var wideView = Custom(WideView, 2, "手牌上限增加 [[amount]]。每回合为最早入休整的 [[count]] 张体力牌恢复 [[restore]] 点体力并将回满的牌移入抽牌堆。",
            "Increase your hand limit by [[amount]]. Each turn, restore [[restore]] Stamina to your [[count]] oldest resting cards and move fully restored cards to the draw pile.",
            power: true, extra: [new("count", 2, Upgradable: false), new("restore", 1, Upgradable: false)]);
        var borrow = Custom(BorrowFromPast, 1, "获得 [[amount]] 层光的背影。回合开始时按休整牌堆中的牌获得格挡。",
            "Gain [[amount]] A Silhouette of Light. At turn start, gain Block based on the Rest Pile.", power: true, fixedAmount: true);
        var reserve = Custom(EnergyReserve, 1, "获得 [[amount]] 层昨日的余力。保留未使用的能量。",
            "Gain [[amount]] Yesterday's Reserves. Keep unspent Energy.", power: true, fixedAmount: true);
        var drawToSize = Custom(DrawToSize, 5, "抽牌直到手牌达到 [[amount]] 张。", "Draw until you have [[amount]] cards in hand.");
        var pierce = Custom(Pierce, 10, "造成 [[amount]] 点无视格挡的伤害。",
            "Deal [[amount]] damage that ignores Block.", damageTarget: "selected_enemy");
        var finishSprint = Custom(FinishSprint, 10, "对所有敌人造成 [[amount]] 点伤害。休整牌堆每有一张牌，额外攻击一次。",
            "Deal [[amount]] damage to ALL enemies. Repeat for each card in your Rest Pile.",
            damageTarget: "all_enemies");
        var finishChain = Custom(FinishChain, 16, "造成 [[amount]] 点伤害两次。若本回合已打出另一张攻击牌，则改为四次。",
            "Deal [[amount]] damage twice, or four times if you already played another Attack this turn.",
            damageTarget: "selected_enemy");
        var winLine = Custom(WinLine, 16, "造成 [[amount]] 点伤害。本回合此前每打出一张攻击牌，伤害增加 [[bonus]] 点，最多计四张。",
            "Deal [[amount]] damage. Add [[bonus]] damage per Attack played earlier this turn, up to four.",
            damageTarget: "selected_enemy", extra: [new("bonus", 8, Upgradable: false)]);
        var restBlock = Custom(RestBlock, 4, "休整牌堆每有一张牌，获得 [[amount]] 点格挡。",
            "Gain [[amount]] Block for each card in your Rest Pile.");

        var damage = Native("ironclad/anger", 0, "damage", 6);
        var block = Native("ironclad/defend_ironclad", 0, "block", 5);
        var clearDistractions = ClearDistractionsEffect();
        var originalRecipes = new[]
        {
            Recipe<CGSStrike>("打击", "Strike", 1, GeneratedCardType.Attack, GeneratedRarity.Basic, [damage]),
            Recipe<CGSDefend>("防御", "Defend", 1, GeneratedCardType.Skill, GeneratedRarity.Basic, [block]),
            Recipe<CGSForwardAStep>("小小的勇气", "A Little Courage", 2, GeneratedCardType.Attack, GeneratedRarity.Basic,
                [Native("ironclad/anger", 0, "damage", 10), Native("ironclad/defend_ironclad", 0, "block", 8)]),
            Recipe<CGSDisrupTheFormation>("影打", "Shadow Swing", 1, GeneratedCardType.Attack, GeneratedRarity.Common,
                [Native("ironclad/anger", 0, "damage", 10), Native("ironclad/uppercut", 1, "amount", 2)]),
            Recipe<CGSPierceTheGap>("穿浪", "Through the Waves", 1, GeneratedCardType.Attack, GeneratedRarity.Common, [pierce.Atom]),
            Recipe<CGSFinalSprint>("伟大的踏破", "A Grand Crossing", 2, GeneratedCardType.Attack, GeneratedRarity.Rare, [finishSprint.Atom]),
            Recipe<CGSFinalChainStep>("超越那束光", "Beyond That Light", 2, GeneratedCardType.Attack, GeneratedRarity.Rare, [finishChain.Atom]),
            Recipe<CGSWinningLine>("触手可及", "Within Reach", 2, GeneratedCardType.Attack, GeneratedRarity.Rare, [winLine.Atom]),
            Recipe<CGSGuardTheFinish>("姐妹的声援", "My Sisters' Cheers", 1, GeneratedCardType.Skill, GeneratedRarity.Uncommon,
                [Native("ironclad/defend_ironclad", 0, "block", 12), restBlock.Atom]),
            Recipe<CGSUnwindMomentum>("躲进帽子里", "Hiding in My Cap", 1, GeneratedCardType.Skill, GeneratedRarity.Common,
                [Native("ironclad/defend_ironclad", 0, "block", 16), Native("ironclad/uppercut", 1, "amount", 6)]),
            Recipe<CGSClamToFight>("无声的决意", "Quiet Resolve", 1, GeneratedCardType.Skill, GeneratedRarity.Common,
            [
                Native("ironclad/defend_ironclad", 0, "block", 8), SelfPower("vigor", 4,
                    "获得 [[amount]] 层活力。", "Gain [[amount]] Vigor.")
            ]),
            Recipe<CGSNeverStepBehind>("不甘的次席", "Not Content with Second", 2, GeneratedCardType.Skill, GeneratedRarity.Rare,
                [Native("ironclad/defend_ironclad", 0, "block", 24), Native("ironclad/thunderclap", 1, "amount", 4)]),
            Recipe<CGSCounterAttack>("我也会还手", "I Can Fight Back, Too", 1, GeneratedCardType.Skill, GeneratedRarity.Common,
                [Native("ironclad/defend_ironclad", 0, "block", 10), counter.Atom]),
            Recipe<CGSBriefRestRestart>("偶尔吃多点", "A Little Extra Sometimes", 1, GeneratedCardType.Skill, GeneratedRarity.Common,
                [Native("ironclad/defend_ironclad", 0, "block", 4), recoverSkills.Atom]),
            Recipe<CGSReorderNotes>("又写满一页", "Another Page Filled", 0, GeneratedCardType.Skill, GeneratedRarity.Common,
                [drawToSize.Atom]),
            Recipe<CGSClearDistractions>("与自卑告别", "Goodbye to Self-Doubt", 1, GeneratedCardType.Skill, GeneratedRarity.Common,
                [clearDistractions.Atom]),
            Recipe<CGSSubZero>("夏凪", "Summer Calm", 2, GeneratedCardType.Power, GeneratedRarity.Rare,
            [
                Native("silent/survivor", 1), SelfPower("plating", 16,
                    "获得 [[amount]] 层覆甲。", "Gain [[amount]] Plating."),
                recoverAll.Atom
            ]),
            Recipe<CGSWideView>("我憧憬的蓝", "The Blue I Dream Of", 2, GeneratedCardType.Power, GeneratedRarity.Rare,
                [wideView.Atom]),
            Recipe<CGSBorrowFromThePast>("光的背影", "A Silhouette of Light", 2, GeneratedCardType.Power, GeneratedRarity.Rare,
                [borrow.Atom]),
            Recipe<CGSKeepSomeInReserve>("昨日的余力", "Yesterday's Reserves", 2, GeneratedCardType.Power, GeneratedRarity.Rare,
                [reserve.Atom])
        };

        var expanded = BuildAdditionalRecipes();
        IroncladCardRecipe[] recipes = [.. originalRecipes, .. expanded.Recipes];
        VerifyCoverage(recipes);
        var catalog = new ImmutableComponentCatalog(Archetype, recipes);
        var profile = new ComponentGenerationProfile(ProfileId + ":normal", Archetype, false, catalog, catalog,
            catalog, () => ComponentApi.CreateNativeOccurrencePolicy(catalog), ComponentApi.DefaultValuePolicy,
            new ComponentKeywordPolicy(AllowedBaseKeywords: new HashSet<GeneratorTag>(),
                AllowedUpgradeAdditions: new HashSet<GeneratorTag>(), UseArchetypeUpgradeDefaults: false));
        CustomEffect[] effects =
        [
            recoverAll, recoverSkills, counter, wideView, borrow, reserve, drawToSize,
            pierce, finishSprint, finishChain, winLine, restBlock, clearDistractions,
            .. expanded.Effects
        ];
        ComponentPackageApi.Register(new ComponentPackageRegistration(ProfileId + ":components", Request, profile,
            // v0.3.165's numeric renderer also registers legacy English variants while sampling fixed values.
            LocalizedTexts: catalog.Atoms.Select(atom => new ComponentLocalizedText(atom.Template, atom.ChineseText,
                atom.LocalizedText!.RenderEnglish(atom.RuntimeSpec!)!)).ToArray(),
            Valuations:
            [
                new(Opcode, "", new ChevalEffectValue()),
                .. expanded.Valuations
            ],
            IncludeInUltimateChaos: false,
            Localizations: effects.Select(effect => effect.Text).ToArray()));
        CardNameGenerator.RegisterExternalParts(ProfileId + ":names",
        [
            new(nameof(CGSStrike), ["打击"], "", "Basic", " Strike"),
            new(nameof(CGSDefend), ["防御"], "", "Defend", ""),
            new(nameof(CGSForwardAStep), ["小小", "勇气"], "Little ", "Courage", ""),
            new(nameof(CGSDisrupTheFormation), ["影", "打"], "Shadow ", "Swing", ""),
            new(nameof(CGSPierceTheGap), ["穿", "浪"], "Through the ", "Waves", ""),
            new(nameof(CGSFinalSprint), ["伟大", "踏破"], "Grand ", "Crossing", ""),
            new(nameof(CGSFinalChainStep), ["超越", "光"], "Beyond the ", "Light", ""),
            new(nameof(CGSWinningLine), ["触手", "可及"], "Within ", "Reach", ""),
            new(nameof(CGSGuardTheFinish), ["姐妹", "声援"], "My Sisters' ", "Cheers", ""),
            new(nameof(CGSUnwindMomentum), ["躲进", "帽子"], "Hiding in ", "Cap", ""),
            new(nameof(CGSClamToFight), ["无声", "决意"], "Quiet ", "Resolve", ""),
            new(nameof(CGSNeverStepBehind), ["不甘", "次席"], "In ", "Second", " Place"),
            new(nameof(CGSCounterAttack), ["还手", "回击"], "Ready to ", "Retaliate", ""),
            new(nameof(CGSBriefRestRestart), ["偶尔", "吃点"], "A Little ", "Extra", ""),
            new(nameof(CGSReorderNotes), ["写满", "一页"], "Another ", "Page", " Filled"),
            new(nameof(CGSClearDistractions), ["告别", "自卑"], "Goodbye to ", "Doubt", ""),
            new(nameof(CGSSubZero), ["夏", "凪"], "Summer ", "Calm", ""),
            new(nameof(CGSWideView), ["憧憬", "蓝"], "Dreaming in ", "Blue", ""),
            new(nameof(CGSBorrowFromThePast), ["光", "背影"], "A Silhouette of ", "Light", ""),
            new(nameof(CGSKeepSomeInReserve), ["昨日", "余力"], "Yesterday's ", "Reserves", "")
        ]);
        RegisterAdditionalNames(expanded.Recipes);
        var handler = new ChevalEffectHandler();
        ComponentRuntimeApi.RegisterPackage(ProfileId + ":runtime",
            effects.Select(effect => new ComponentRuntimeRoute(Opcode, effect.Atom.RuntimeSpec!.Variant, handler)));
        ComponentPresentationApi.RegisterPackage(ProfileId + ":presentation",
            [new(Opcode, "", new ChevalEffectTips())]);

        foreach (var type in ChevalChaosSlots.Layout.Select(slot => slot.Type).Distinct())
        {
            var shells = new ImmutableComponentCatalog(Archetype, recipes.Where(recipe => recipe.Type == type));
            var request = RequestFor(type);
            ComponentApi.RegisterProfile(request, new ComponentGenerationProfile(request.ProfileId, Archetype,
                false, shells, catalog, catalog, () => ComponentApi.CreateNativeOccurrencePolicy(catalog),
                ComponentApi.DefaultValuePolicy, profile.KeywordPolicy));
        }
    }

    private static IroncladCardRecipe Recipe<T>(string chinese, string english, int cost, GeneratedCardType type,
        GeneratedRarity rarity, ComponentAtom[] atoms) where T : CardModel =>
        new(typeof(T).Name, chinese, cost, type,
            atoms.Any(atom => atom.RequiresSingleTarget) ? TargetMode.SingleEnemy : TargetMode.Other,
            rarity, [], atoms, Enumerable.Repeat(-1, atoms.Length).ToArray(), EnglishTitle: english);

    private static ComponentAtom Native(string id, int index, string? valueSlot = null, int value = 0)
    {
        if (!NativeCardDecompositionApi.TryCreateBaseDefinition("native/" + id, out var card))
            throw new InvalidOperationException($"AutoAnthony recipe {id} is unavailable.");

        var operation = card.Operations[index];
        var spec = OperationRuntimeSpecCompiler.RequireStructured(operation);
        if (valueSlot is not null)
        {
            if (!spec.Values.Any(slot => slot.Id == valueSlot))
                throw new InvalidOperationException($"AutoAnthony recipe {id} does not have a {valueSlot} slot.");

            spec = spec with
            {
                Values = spec.Values.Select(slot => slot.Id == valueSlot ? slot with { BaseValue = value } : slot).ToArray()
            };
        }
        var localized = operation.LocalizedText
            ?? throw new InvalidOperationException($"AutoAnthony recipe {id} lacks its localization template.");
        return new ComponentAtom(operation.Template, operation.Scope, localized.RenderChinese(spec),
            operation.RequiresSingleTarget, CardReferenceRequirement.None)
        {
            SemanticId = $"chevalgrandslay/{id}/{index}/{value}",
            RuntimeSpec = spec,
            LocalizedText = localized
        };
    }

    private static ComponentAtom SelfPower(string variant, int amount, string chinese, string english)
    {
        var spec = new OperationRuntimeSpec(OperationRuntimeSpec.CurrentSchemaVersion, "apply_power", variant, "self",
            "none", "none", "any",
            [ComponentSemanticFlags.Beneficial, ComponentSemanticFlags.PowerFoundation, ComponentSemanticFlags.ScalableReward],
            [new("amount", amount)]);
        var localized = new OperationLocalizedText(chinese, english);
        return new("N:Self", OperationScope.NonTargeted, localized.RenderChinese(spec), false, CardReferenceRequirement.None)
        {
            SemanticId = "chevalgrandslay/" + variant,
            RuntimeSpec = spec,
            LocalizedText = localized
        };
    }

    private static CustomEffect Custom(string variant, int value, string chinese, string english,
        bool rest = false, bool power = false, bool fixedAmount = false, RuntimeValueSlot[]? extra = null,
        string? damageTarget = null, bool requiresChoice = false, bool downside = false,
        string? target = null, bool block = false)
    {
        var semanticId = "chevalgrandslay/" + variant;
        var flags = downside
            ? new List<string> { ComponentSemanticFlags.Negative }
            : new List<string> { ComponentSemanticFlags.Beneficial, ComponentSemanticFlags.ScalableReward };
        if (power)
            flags.Add(ComponentSemanticFlags.PowerFoundation);
        if (damageTarget is not null)
            flags.Add(ComponentSemanticFlags.EnemyDamage);
        if (requiresChoice || variant == RecoverSkills)
            flags.Add("requires_player_choice");
        if (block)
            flags.Add("block_reference");
        var spec = new OperationRuntimeSpec(OperationRuntimeSpec.CurrentSchemaVersion, Opcode, variant, target ?? damageTarget ?? "self",
            rest ? RestZone : "none", rest ? "draw" : "none", "any", flags,
            [
                new("amount", value, Upgradable: !fixedAmount,
                    Explicit: chinese.Contains("[[amount]]", StringComparison.Ordinal)),
                .. extra ?? []
            ]);
        var localized = new OperationLocalizedText(chinese, english);
        bool needsTarget = (target ?? damageTarget) == "selected_enemy";
        ComponentAtom atom = new("cgs:" + variant,
            needsTarget ? OperationScope.SingleEnemyOnly : OperationScope.NonTargeted, localized.RenderChinese(spec),
            needsTarget, CardReferenceRequirement.None)
        {
            SemanticId = semanticId,
            RuntimeSpec = spec,
            LocalizedText = localized
        };
        return new(atom, new(semanticId, chinese, english));
    }

    private sealed class ChevalEffectValue : IComponentValuation
    {
        public int Estimate(ComponentValuationContext context)
        {
            int amount = context.Value("amount");
            int estimate = context.RuntimeSpec.Variant switch
            {
                RecoverAll => amount * 300,
                RecoverSkills => amount * 225,
                Counter => amount * 80,
                WideView => 700 + 250 * amount,
                BorrowFromPast => 1200,
                EnergyReserve => 1500,
                DrawToSize => (amount - 2) * 150,
                Pierce => amount * 125,
                FinishSprint or FinishChain => amount * 300,
                WinLine => amount * 100 + context.Value("bonus") * 180,
                RestBlock => amount * 150,
                "exhaust_status_draw" => amount * 65,
                _ => throw new InvalidOperationException($"Unknown Cheval component {context.RuntimeSpec.Variant}.")
            };
            // The fitter probes zero-valued variants even when it selects a positive final value.
            return Math.Max(1, estimate);
        }
    }

    private sealed class ChevalEffectHandler : IComponentRuntimeHandler
    {
        public async Task<bool> ExecuteAsync(ComponentRuntimeContext context)
        {
            var source = context.Card;
            var player = source.Owner;
            var owner = player.Creature;
            var choices = context.ChoiceContext;
            int amount = context.Amount;
            switch (context.RuntimeSpec.Variant)
            {
                case RecoverAll:
                    await CGSStamina.RecoverRestingCardsToDraw(Entry.CGSFatePile.GetPile(player).Cards, amount);
                    return true;
                case Pierce:
                    if (context.Target is { IsAlive: true } pierced)
                    {
                        await CreatureCmd.Damage(choices, pierced, amount, ValueProp.Move | ValueProp.Unblockable,
                            owner, source, context.CardPlay);
                    }
                    return true;
                case FinishSprint:
                    if (source.CombatState is { } combat)
                    {
                        int hits = 1 + Entry.CGSFatePile.GetPile(player).Cards.Count;
                        await DamageCmd.Attack(amount).WithHitCount(hits).FromCard(source, context.CardPlay)
                            .CGSWithVfx("vfx/vfx_attack_slash").TargetingAllOpponents(combat).Execute(choices);
                    }
                    return true;
                case FinishChain:
                    if (context.Target is { IsAlive: true } chainTarget)
                    {
                        int hits = CGSAttackChain.CountAttacksPlayedThisTurn(player, source.CombatState, context.CardPlay) > 0 ? 4 : 2;
                        for (int hit = 0; hit < hits; hit++)
                            await DamageCmd.Attack(amount).FromCard(source, context.CardPlay)
                                .CGSWithVfx("vfx/vfx_attack_slash").Targeting(chainTarget).Execute(choices);
                    }
                    return true;
                case WinLine:
                    if (context.Target is { IsAlive: true } lineTarget)
                    {
                        int attacks = Math.Min(4, CGSAttackChain.CountAttacksPlayedThisTurn(player, source.CombatState, context.CardPlay));
                        int bonus = context.RuntimeSpec.Values.Single(slot => slot.Id == "bonus").BaseValue;
                        await DamageCmd.Attack(amount + bonus * attacks).FromCard(source, context.CardPlay)
                            .CGSWithVfx("vfx/vfx_attack_slash").Targeting(lineTarget).Execute(choices);
                    }
                    return true;
                case RestBlock:
                    int block = amount * Entry.CGSFatePile.GetPile(player).Cards.Count;
                    if (block > 0)
                        await CreatureCmd.GainBlock(owner, block, ValueProp.Move, context.CardPlay);
                    return true;
                case RecoverSkills:
                    await CGSBriefRestRestart.RecoverSkills(choices, player, amount);
                    return true;
                case Counter:
                    await PowerCmd.Apply<CGSCounterAttackPower>(choices, owner, amount, owner, source);
                    return true;
                case WideView:
                    var wideView = await PowerCmd.Apply<CGSWideViewPower>(choices, owner, amount, owner, source);
                    var values = context.RuntimeSpec.Values;
                    wideView?.SetRecovery(values.Single(slot => slot.Id == "count").BaseValue,
                        values.Single(slot => slot.Id == "restore").BaseValue);
                    return true;
                case BorrowFromPast:
                    await PowerCmd.Apply<CGSBorrowFromThePastPower>(choices, owner, 1m, owner, source);
                    return true;
                case EnergyReserve:
                    await PowerCmd.Apply<CGSEnergyReservePower>(choices, owner, 1m, owner, source);
                    return true;
                case DrawToSize:
                    int count = Math.Max(0, amount - PileType.Hand.GetPile(player).Cards.Count);
                    if (count > 0)
                        context.ReplaceDrawnCards((await CardPileCmd.Draw(choices, count, player)).ToArray());
                    return true;
                default:
                    return await ExecuteAdditionalAsync(context);
            }
        }
    }

    private sealed class ChevalEffectTips : IComponentHoverTipProvider
    {
        public IEnumerable<IHoverTip> BuildHoverTips(ComponentPresentationContext context) =>
            context.RuntimeSpec.Variant switch
            {
                RecoverAll or RecoverSkills or FinishSprint or RestBlock => [ModKeywordRegistry.CreateHoverTip(CGSKeywords.RestPile)],
                Counter => [HoverTipFactory.FromPower<CGSCounterAttackPower>()],
                WideView => [HoverTipFactory.FromPower<CGSWideViewPower>(), ModKeywordRegistry.CreateHoverTip(CGSKeywords.RestPile)],
                BorrowFromPast => [HoverTipFactory.FromPower<CGSBorrowFromThePastPower>(), ModKeywordRegistry.CreateHoverTip(CGSKeywords.RestPile)],
                EnergyReserve => [HoverTipFactory.FromPower<CGSEnergyReservePower>()],
                _ => AdditionalHoverTips(context)
            };
    }
}
