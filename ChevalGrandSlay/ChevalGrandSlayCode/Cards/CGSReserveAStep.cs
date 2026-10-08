using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 预留一步
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSReserveAStep : CGSStaminaCardTemplate
{
    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [
            new DynamicVar("Uses", 3m), 
            new DynamicVar("MaxUses", 3m), 
            new BlockVar(5m, ValueProp.Move)
        ];

    public CGSReserveAStep() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self, true)
    {
        
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        var candidates = PileType.Hand.GetPile(Owner).Cards
            .Where(candidatesCard => candidatesCard != this && candidatesCard.DynamicVars.ContainsKey("Uses") && candidatesCard.DynamicVars["Uses"].IntValue > 0)
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }
        
        var selected = await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), candidates.Contains, this);
        
        if (selected.FirstOrDefault() is { } card)
        {
            var power = await PowerCmd.Apply<CGSReserveAStepPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
            power?.SetCard(card, Owner?.PlayerCombatState?.TurnNumber ?? -1);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        CGSConsumeUse.GetResultPileTypeForCardPlay(this, base.GetResultPileTypeForCardPlay());

}
