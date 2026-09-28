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

// 算清代价
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSCountTheCost : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new();
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Uses", 2m), new DynamicVar("MaxUses", 2m), new CardsVar(2)];

    public CGSCountTheCost() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner ?? throw new InvalidOperationException("This card needs an owner.");
        ConsumeUse();

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, player);
        if (player.PlayerCombatState is { } state && _fateEnteredTurn == state.TurnNumber)
            await PlayerCmd.GainEnergy(1m, player);
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


    private int _fateEnteredTurn = -1;

    public override Task AfterCardChangedPiles(CardModel card, PileType previousPile, AbstractModel? source)
    {
        if (Owner is { PlayerCombatState: { } state } player && card.Owner == player &&
            card.Pile?.Type == Entry.CGSFatePile && previousPile != Entry.CGSFatePile)
            _fateEnteredTurn = state.TurnNumber;
        return Task.CompletedTask;
    }
}
