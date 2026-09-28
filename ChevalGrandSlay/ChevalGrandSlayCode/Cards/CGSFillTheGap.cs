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

// 弥补空白
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSFillTheGap : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new();
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public CGSFillTheGap() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CGSFillTheGapPower>(choiceContext, Owner.Creature,
            IsUpgraded ? 2m : 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {

    }
}

[RegisterPower]
public sealed class CGSFillTheGapPower : ModPowerTemplate
{
    private int _triggeredTurn = -1;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new();

    public override async Task AfterCardChangedPiles(CardModel card, PileType previousPile, AbstractModel? source)
    {
        if (card.Owner != Owner.Player || previousPile != Entry.CGSFatePile ||
            card.Pile?.Type == Entry.CGSFatePile || !card.DynamicVars.ContainsKey("Uses") ||
            CombatState?.CurrentSide != CombatSide.Player ||
            Owner.Player?.PlayerCombatState is not { } state || _triggeredTurn == state.TurnNumber)
            return;
        _triggeredTurn = state.TurnNumber;
        var uses = card.DynamicVars["Uses"];
        uses.BaseValue = Math.Min(card.DynamicVars["MaxUses"].BaseValue, uses.BaseValue + 1m);
        if (Amount > 1m) await PlayerCmd.GainEnergy(1m, Owner.Player);
    }
}
