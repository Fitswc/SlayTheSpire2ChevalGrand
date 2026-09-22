using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 一鼓作气
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSAllInOneGo : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        SecondaryResourceVars.For("Determination", CGSDetermination.CGSDeterminationId, 6m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override bool IsPlayable =>
        base.IsPlayable && CGSAttackChain.CountAttacksPlayedThisTurn(Owner, CombatState) >= 2;

    public CGSAllInOneGo() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SecondaryResourceCmd.Gain(
            Owner,
            CGSDetermination.CGSDeterminationId,
            DynamicVars["Determination"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Determination"].UpgradeValueBy(3m);
    }
}