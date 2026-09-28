using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 借势卸困
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSBorrowForceEscape : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Reduction", 1m)];
    public CGSBorrowForceEscape() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true)
    {
    }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        await PowerCmd.Apply<CGSResistWeakFrailPower>(choiceContext, Owner.Creature,
            DynamicVars["Reduction"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars["Reduction"].UpgradeValueBy(1m);
}

[RegisterPower]
public sealed class CGSResistWeakFrailPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new();

    public override bool TryModifyPowerAmountReceived(PowerModel power, Creature target,
        decimal amount, Creature? applier, out decimal modified)
    {
        modified = target == Owner && amount > 0m &&
            (power is WeakPower or FrailPower) ? Math.Max(0m, amount - Amount) : amount;
        return modified != amount;
    }
}
