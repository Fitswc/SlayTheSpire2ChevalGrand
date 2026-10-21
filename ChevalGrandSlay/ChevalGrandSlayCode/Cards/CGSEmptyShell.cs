using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 体力耗尽的卡牌被复制时产生的空壳。
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSEmptyShell : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new();
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public CGSEmptyShell() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self, false) { }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;
}
