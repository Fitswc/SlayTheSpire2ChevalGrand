using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 命运回声
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSFateEcho : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new();
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Uses", 2m), new DynamicVar("MaxUses", 2m), new CardsVar(1)];

    public CGSFateEcho() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ConsumeUse();

        var candidates = Entry.CGSFatePile.GetPile(Owner).Cards.Where(card => card.DynamicVars.ContainsKey("Uses")).ToList();
        if (candidates.Count > 0)
        {
            var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, candidates, Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1));
            if (selected.FirstOrDefault() is { } card)
            {
                var uses = card.DynamicVars["Uses"];
                uses.BaseValue = Math.Min(card.DynamicVars["MaxUses"].BaseValue, uses.BaseValue + 1m);
                await CardPileCmd.Add(card, PileType.Discard);
            }
        }
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        DynamicVars.Cards.UpgradeValueBy(1m);
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

