using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 重整手札
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSReorderNotes : CGSLimitedUseCard
{
    public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(UsesVarName, 4m), new DynamicVar("HandSize", 5m)];

    public CGSReorderNotes() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int needed = Math.Max(0, DynamicVars["HandSize"].IntValue - PileType.Hand.GetPile(Owner).Cards.Count);
        if (needed > 0) await CardPileCmd.Draw(choiceContext, needed, Owner);
        ConsumeUse(cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars["HandSize"].UpgradeValueBy(1m);
}
