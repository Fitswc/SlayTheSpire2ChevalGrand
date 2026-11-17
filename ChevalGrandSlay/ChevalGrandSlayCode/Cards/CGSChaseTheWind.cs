using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using ChevalGrandSlay.Utilities;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Combat.HandSize;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卡牌：衣角逐风
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSChaseTheWind : CGSStaminaCard
{
    // 基础耗能。
    private const int BaseEnergyCost = 1;

    // 卡牌类型。
    private const CardType CardKind = CardType.Attack;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Uncommon;

    // 目标类型（AnyEnemy 表示任意敌人）。
    private const TargetType CardTarget = TargetType.AnyEnemy;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    public CGSChaseTheWind() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CGSKeywords.Stamina];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [ModKeywordRegistry.CreateHoverTip(CGSKeywords.RestPile)];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ModCardVars.Damage(10m, ValueProp.Move),
        ModCardVars.Cards(1),
        new DynamicVar(CGSStamina.Uses, 4m),
        new DynamicVar(CGSStamina.MaxUses, 4m)
    ];

    protected override async Task OnStaminaPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay).CGSWithVfx("vfx/vfx_attack_slash")
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        if (CombatState is not { } combatState || CombatManager.Instance.IsOverOrEnding)
            return;

        var options = new List<CardModel>();
        AddPileOption<CGSChaseTheWindDrawPile>(options, PileType.Draw);
        AddPileOption<CGSChaseTheWindDiscardPile>(options, PileType.Discard);
        AddPileOption<CGSChaseTheWindRestPile>(options, Entry.CGSFatePile);
        if (options.Count == 0)
            return;

        var selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, Owner, canSkip: false);
        if (selected is not CGSChaseTheWindPileChoice choice)
            return;

        int count = DynamicVars.Cards.IntValue;
        var pile = choice.SourcePile.GetPile(Owner);
        if (choice.SourcePile == PileType.Draw)
            await CardPileCmd.Draw(choiceContext, Math.Min(count, pile.Cards.Count), Owner);
        else
            await DrawFromPile(choiceContext, combatState, pile, count);
    }

    private void AddPileOption<T>(List<CardModel> options, PileType pile) where T : CGSChaseTheWindPileChoice
    {
        if (pile.GetPile(Owner).Cards.Count == 0)
            return;

        var option = ModelDb.Card<T>().ToMutable();
        option.Owner = Owner;
        options.Add(option);
    }

    private async Task DrawFromPile(PlayerChoiceContext choiceContext, ICombatState combatState, CardPile pile, int count)
    {
        if (!Hook.ShouldDraw(combatState, Owner, false, out var modifier))
        {
            if (modifier is not null)
                await Hook.AfterPreventingDraw(combatState, modifier);
            return;
        }

        var candidates = pile.Cards.ToList();
        var hand = PileType.Hand.GetPile(Owner);
        var combatManager = CombatManager.Instance;
        for (int i = 0; i < count && candidates.Count > 0; i++)
        {
            if (combatManager.IsOverOrEnding || hand.Cards.Count >= MaxHandSizeCalculator.Calculate(Owner))
                break;

            var card = Owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
            if (card is null)
                break;

            candidates.Remove(card);
            if (card.Pile != pile || !(await CardPileCmd.Add(card, PileType.Hand)).success)
                continue;

            combatManager.History.CardDrawn(combatState, card, false);
            await Hook.AfterCardDrawn(combatState, choiceContext, card, false);
            card.InvokeDrawn();
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

public abstract class CGSChaseTheWindPileChoice()
    : ModCardTemplate(0, CardType.Skill, CardRarity.Token, TargetType.Self, false)
{
    internal abstract PileType SourcePile { get; }
    public override bool CanBeGeneratedInCombat => false;
    protected override bool IsPlayable => false;

    public override CardAssetProfile AssetProfile =>
        new(PortraitPath: $"{Entry.ResPath}/images/cards/CGSChaseTheWind.png");

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;
}

[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSChaseTheWindDrawPile : CGSChaseTheWindPileChoice
{
    internal override PileType SourcePile => PileType.Draw;
}

[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSChaseTheWindDiscardPile : CGSChaseTheWindPileChoice
{
    internal override PileType SourcePile => PileType.Discard;
}

[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSChaseTheWindRestPile : CGSChaseTheWindPileChoice
{
    internal override PileType SourcePile => Entry.CGSFatePile;
}
