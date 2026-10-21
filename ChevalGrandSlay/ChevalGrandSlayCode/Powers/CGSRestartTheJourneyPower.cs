using ChevalGrandSlay.Keywords;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSRestartTheJourneyPower : ModPowerTemplate
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromKeyword(CGSKeywords.RestPile)
    ];

    private int _triggerCount;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    // 自定义图标路径。1:1即可。原版游戏大图256x256，小图64x64。
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/CGSRestartTheJourneyPower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/CGSRestartTheJourneyPower.png"
    );

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || _triggerCount >= Amount) return;
        var candidates = Entry.CGSFatePile.GetPile(player).Cards
            .Where(card => card.DynamicVars.ContainsKey("Uses")).ToList();
        if (candidates.Count == 0) return;
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, candidates, player,
            new CardSelectorPrefs(SelectionScreenPrompt, 1));
        if (selected.FirstOrDefault() is not { } card) return;
        var uses = card.DynamicVars["Uses"];
        uses.BaseValue = Math.Min(card.DynamicVars["MaxUses"].BaseValue, uses.BaseValue + 1m);
        await CardPileCmd.Add(card, PileType.Discard);
        _triggerCount++;
    }
}
