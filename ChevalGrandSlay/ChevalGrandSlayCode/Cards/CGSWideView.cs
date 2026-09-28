using STS2RitsuLib.Combat.HandSize;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 宽阔视野
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSWideView : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HandLimitBonus", 2m)
    ];

    public CGSWideView() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {

        await PowerCmd.Apply<CGSWideViewPower>(choiceContext, Owner.Creature,
            DynamicVars["HandLimitBonus"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["HandLimitBonus"].UpgradeValueBy(1m);
}

[RegisterPower]
public sealed class CGSWideViewPower : ModPowerTemplate, IMaxHandSizeModifier
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new();

    public override decimal ModifyHandDraw(Player player, decimal amount) =>
        player == Owner.Player ? amount + 1m : amount;

    public int ModifyMaxHandSize(Player player, int currentMaxHandSize) =>
        player == Owner.Player ? currentMaxHandSize + (int)Amount : currentMaxHandSize;
}
