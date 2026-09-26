using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 倾注一式
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSPourIntoOneMove : ModCardTemplate
{
    public override CardAssetProfile AssetProfile =>
        new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override bool IsPlayable => base.IsPlayable && Owner is not null &&
                                          PileType.Hand.GetPile(Owner).Cards.Any(IsCandidate);

    public CGSPourIntoOneMove() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy, true)
    {
        this.SecondaryResourceUses().Require("determination", CGSDetermination.CGSDeterminationId, 6);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), IsCandidate, this);
        if (selected.FirstOrDefault() is not CGSLimitedUseCard attack) return;
        if (IsUpgraded)
            await PlayerCmd.LoseEnergy(attack.EnergyCost.GetAmountToSpend(), Owner);
        attack.ConsumeAllUses();
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var power = await PowerCmd.Apply<CGSFirstStrikeTwicePower>(choiceContext, Owner.Creature,
            1m, Owner.Creature, this);
        power?.SetAttack(attack);
        try
        {
            await CardCmd.AutoPlay(choiceContext, attack, cardPlay.Target);
        }
        finally
        {
            if (power is not null)
                await PowerCmd.Remove(power);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    private bool IsCandidate(CardModel card) =>
        card is CGSLimitedUseCard { RemainingUses: >= 2 } && card.Type == CardType.Attack &&
        (!IsUpgraded || (Owner.PlayerCombatState is { } state &&
                         card.EnergyCost.GetAmountToSpend() <= state.Energy));
}