using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 留有余裕
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSKeepSomeInReserve : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("RetainEnergy", 1m)];

    public CGSKeepSomeInReserve() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        await PowerCmd.Apply<CGSEnergyReservePower>(choiceContext, Owner.Creature,
            DynamicVars["RetainEnergy"].BaseValue, Owner.Creature, this);

    protected override void OnUpgrade() => DynamicVars["RetainEnergy"].UpgradeValueBy(1m);
}

[RegisterPower]
public sealed class CGSEnergyReservePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new();

    private int _saved;

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player && participants.Contains(Owner) &&
            Owner.Player?.PlayerCombatState is { } state)
            _saved = Math.Min(Math.Max(0, state.Energy), (int)Amount);
        return Task.CompletedTask;
    }

    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner.Player || _saved == 0) return;
        int energy = _saved;
        _saved = 0;
        await PlayerCmd.GainEnergy(energy, player);
    }
}
