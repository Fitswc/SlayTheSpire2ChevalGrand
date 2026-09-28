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

// 划定终线
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSDrawFinishLine : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new();
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Uses", 2m), new DynamicVar("MaxUses", 2m), new CardsVar(1)];

    public CGSDrawFinishLine() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ConsumeUse();

        var selected = await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            card => card != this && card.DynamicVars.ContainsKey("Uses") && card.DynamicVars["Uses"].IntValue > 0, this);
        if (selected.FirstOrDefault() is not { } card) return;
        card.DynamicVars["Uses"].BaseValue = 0m;
        await CardPileCmd.Add(card, Entry.CGSFatePile);
        await PlayerCmd.GainEnergy(2m, Owner);
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

