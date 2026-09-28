using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 瞄准终点
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSAimForTheFinish : ModCardTemplate
{
    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Uses", 2m), new DynamicVar("MaxUses", 2m)];

    public CGSAimForTheFinish() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ConsumeUse();
        var candidates = PileType.Draw.GetPile(Owner).Cards
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Where(card => card.Type == CardType.Skill).ToList();
        if (candidates.Count == 0)
            return;
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, candidates, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1));
        foreach (var card in selected)
        {
            await CardPileCmd.Add(card, PileType.Hand);
            card.EnergyCost.AddThisTurn(-1);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        EnergyCost.UpgradeBy(-1);
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
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
