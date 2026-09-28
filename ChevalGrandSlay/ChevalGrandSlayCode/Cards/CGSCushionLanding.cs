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

// 缓冲落点
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSCushionLanding : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new();
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Uses", 3m), new DynamicVar("MaxUses", 3m), new BlockVar(6m, ValueProp.Move)];

    public CGSCushionLanding() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner ?? throw new InvalidOperationException("This card needs an owner.");
        ConsumeUse();

        await CreatureCmd.GainBlock(player.Creature, DynamicVars.Block, cardPlay);
        if (player.PlayerCombatState is { } state && _fateEnteredTurn == state.TurnNumber)
            await CardPileCmd.Draw(choiceContext, 1m, player);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        DynamicVars.Block.UpgradeValueBy(3m);
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
