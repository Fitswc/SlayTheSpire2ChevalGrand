using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 运动饮料补给
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSSportsDrinkSupply : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new();
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public CGSSportsDrinkSupply() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = IsUpgraded ? 3 : 2;
        for (int i = 0; i < count; i++)
        {
            var drink = (CardModel)ModelDb.Card<CGSEnergyDrink>().MutableClone();
            drink.Owner = Owner;
            await CardPileCmd.Add(drink, PileType.Hand);
        }
    }

    protected override void OnUpgrade()
    {

    }
}
