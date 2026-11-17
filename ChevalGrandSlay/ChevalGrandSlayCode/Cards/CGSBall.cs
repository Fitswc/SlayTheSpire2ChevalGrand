using ChevalGrandSlay.Utilities;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class CGSBall() : ModCardTemplate(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, false)
{
    public override bool CanBeGeneratedInCombat => false;
    public override int MaxUpgradeLevel => 0;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/CGSLendReserve.png");

    protected override IEnumerable<DynamicVar> CanonicalVars => [ModCardVars.Damage(8m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .CGSWithVfx("vfx/vfx_dagger_throw")
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }
}
