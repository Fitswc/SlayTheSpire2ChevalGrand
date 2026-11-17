using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卡牌：发胖！
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSWeightGain() : ModCardTemplate(1, CardType.Status, CardRarity.Status, TargetType.None, false)
{
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;
    private const bool ShowInCardLibrary = false;

    public override CardAssetProfile AssetProfile =>
        new(PortraitPath: $"{Entry.ResPath}/images/cards/CGSWeightGain.png");

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;
}
