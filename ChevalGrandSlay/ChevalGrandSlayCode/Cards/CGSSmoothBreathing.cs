using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 顺畅呼吸
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSSmoothBreathing : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(2)];

    public CGSSmoothBreathing() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CGSSmoothBreathingPower>(choiceContext, Owner.Creature,
            DynamicVars.Cards.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
        EnergyCost.UpgradeBy(-1);
    }
}

[RegisterPower]
public sealed class CGSSmoothBreathingPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new();

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        var candidates = PileType.Draw.GetPile(player).Cards
            .Take((int)Amount)
            .ToList();
        if (candidates.Count == 0)
            return;

        var selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            player,
            new CardSelectorPrefs(SelectionScreenPrompt, 1));
        foreach (var card in selected)
            await CardPileCmd.Add(card, PileType.Hand);
    }
}
