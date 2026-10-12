using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 最后的末脚

//TODO:修复费用
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSFinalKick : ModCardTemplate
{
    public override int MaxUpgradeLevel => 0;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(20171126m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromKeyword(CGSKeywords.Stamina)
    ];

    protected override bool IsPlayable => Owner?.PlayerCombatState is { Energy: 0 } &&
        !PileType.Draw.GetPile(Owner).Cards.Any(card => card.DynamicVars.ContainsKey("Uses")) &&
        !PileType.Discard.GetPile(Owner).Cards.Any(card => card.DynamicVars.ContainsKey("Uses")) &&
        Entry.CGSFatePile.GetPile(Owner).Cards.Count > 0;

    protected override bool ShouldGlowGoldInternal => IsPlayable;

    public CGSFinalKick() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Auto-play can bypass the normal playability check.
        if (!IsPlayable || CombatState is not { } combatState)
            return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this).TargetingAllOpponents(combatState).Execute(choiceContext);
    }
}
