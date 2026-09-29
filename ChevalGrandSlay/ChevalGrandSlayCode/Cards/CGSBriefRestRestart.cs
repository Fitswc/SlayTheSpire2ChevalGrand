using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 小憩再启
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSBriefRestRestart : ModCardTemplate
{
    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [
            new DynamicVar("Uses", 2m),
            new DynamicVar("MaxUses", 2m), 
            new BlockVar(4m, ValueProp.Move)
        ];

    public CGSBriefRestRestart() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self, true)
    {
        
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        
        var candidates = Entry.CGSFatePile.GetPile(Owner).Cards
            .Where(selectedCard => selectedCard.Type == CardType.Skill && selectedCard.DynamicVars.ContainsKey("Uses"))
            .ToList();
        
        if (candidates.Count == 0)
        {
            return;
        }
        
        var selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext, 
            candidates, 
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, 1)
            );
        
        if (selected.FirstOrDefault() is not { } card)
        {
            return;
        }
        
        var uses = card.DynamicVars["Uses"];
        uses.BaseValue = Math.Min(card.DynamicVars["MaxUses"].BaseValue, uses.BaseValue + 1m);
        await CardPileCmd.Add(card, PileType.Discard);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        DynamicVars.Block.UpgradeValueBy(3m);
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        DynamicVars["Uses"].IntValue <= 0 ? Entry.CGSFatePile : base.GetResultPileTypeForCardPlay();



}

