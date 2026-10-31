using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Potions;

[RegisterPotion(typeof(CGSPotionPool))]
public sealed class CGSPotentialPotion : ModPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    public override PotionAssetProfile AssetProfile => new(
        ImagePath: "res://images/potions/CGSPotentialPotion.png",
        OutlinePath: "res://images/potions/CGSPotentialPotion.png");

    /*
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromKeyword(CGSKeywords.Stamina),
        HoverTipFactory.FromKeyword(CardKeyword.Retain)
    ];
    */

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var candidates = ModelDb.CardPool<CGSCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(IsEligibleStaminaCard);
        // The factory creates distinct cards in CombatState, never in the permanent deck.
        var options = CardFactory.GetDistinctForCombat(
            Owner, candidates, 3, Owner.RunState.Rng.CombatCardGeneration).ToList();
        if (options.Count == 0)
            return;

        var selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, Owner, canSkip: false);
        if (selected is null)
            return;

        ApplyBonuses(selected);
        await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner);
    }

    private static bool IsEligibleStaminaCard(CardModel card) =>
        card.CanBeGeneratedInCombat && card.CanonicalKeywords.Contains(CGSKeywords.Stamina) &&
        card.DynamicVars.ContainsKey("Uses") && card.DynamicVars.ContainsKey("MaxUses");

    private static void ApplyBonuses(CardModel card)
    {
        card.EnergyCost.SetThisCombat(0);
        card.DynamicVars["MaxUses"].BaseValue = 999m;
        card.DynamicVars["Uses"].BaseValue = 999m;
        card.AddKeyword(CardKeyword.Retain);
    }
}
