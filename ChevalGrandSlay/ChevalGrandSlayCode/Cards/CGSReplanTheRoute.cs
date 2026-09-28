using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 重编路线
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSReplanTheRoute : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Uses", 3m), new DynamicVar("MaxUses", 3m), new DynamicVar("ShuffleCount", 3m), new CardsVar(1)];

    public CGSReplanTheRoute() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ConsumeUse();
        var discarded = PileType.Discard.GetPile(Owner).Cards.ToList();
        if (discarded.Count > 0)
        {
            var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, discarded, Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 0,
                    Math.Min(DynamicVars["ShuffleCount"].IntValue, discarded.Count)));
            foreach (var card in selected)
                await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Random);
        }
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        EnergyCost.UpgradeBy(-1);
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        DynamicVars["Uses"].IntValue <= 0 ? Entry.CGSFatePile : base.GetResultPileTypeForCardPlay();

    private void ConsumeUse()
    {
        if (Owner.Creature.Powers.OfType<CGSReserveAStepPower>().Any(power => power.TryPreserve(this)))
            return;
        var uses = DynamicVars["Uses"];
        uses.BaseValue = Math.Max(0m, uses.BaseValue - 1m);
    }
}
