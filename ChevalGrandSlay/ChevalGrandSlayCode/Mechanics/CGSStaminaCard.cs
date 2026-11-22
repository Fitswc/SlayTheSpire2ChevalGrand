using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Mechanics;

public abstract class CGSStaminaCard(int energyCost, CardType type, CardRarity rarity, TargetType target)
    : ModCardTemplate(energyCost, type, rarity, target)
{
    protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner is null)
            throw new InvalidOperationException("This card needs an owner.");

        if (CGSStamina.IsEmptyCopy(this))
            return;

        CGSConsumeUse.Consume(this);
        await OnStaminaPlay(choiceContext, cardPlay);
    }

    protected abstract Task OnStaminaPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay);
}
