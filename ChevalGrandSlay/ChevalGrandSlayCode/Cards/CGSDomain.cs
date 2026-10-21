using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 领域

//TODO:修复费用
//
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSDomain : ModCardTemplate
{
    public override int MaxUpgradeLevel => 0;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Turns", 2m),
        new DynamicVar("DamageBonus", CGSDomainPower.DamageBonusPercent),
        new EnergyVar(CGSDomainPower.EnergyPerTurn)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromKeyword(CGSKeywords.RestPile),
        HoverTipFactory.FromPower<CGSDomainPower>(DynamicVars["Turns"].IntValue),
        HoverTipFactory.FromKeyword(CGSKeywords.Stamina)
    ];

    protected override bool IsPlayable => Owner is not null && !Owner.Creature.HasPower<CGSDomainPower>();

    public CGSDomain() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Re-entering an active domain must not extend its death deadline or repeat entry rewards.
        if (Owner.Creature.HasPower<CGSDomainPower>())
            return;

        await PowerCmd.Apply<CGSDomainPower>(choiceContext, Owner.Creature,
            DynamicVars["Turns"].BaseValue, Owner.Creature, this);
    }
}
