using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Mechanics;

public abstract class CGSStaminaCard(int energyCost, CardType type, CardRarity rarity, TargetType target)
    : ModCardTemplate(energyCost, type, rarity, target)
{
    // 体力牌统一走这个出牌入口。先确认持有者并结算体力，再执行该牌的效果。
    protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner is null)
            throw new InvalidOperationException("This card needs an owner.");

        CGSConsumeUse.Consume(this);
        await OnStaminaPlay(choiceContext, cardPlay);
    }

    // 子类在此实现自己的出牌效果，体力消耗由基类处理。
    protected abstract Task OnStaminaPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay);
}
