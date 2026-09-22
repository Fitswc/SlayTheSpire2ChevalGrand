using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Mechanics;

public abstract class CGSLimitedUseCard : ModCardTemplate
{
    public const string UsesVarName = "Uses";

    public int RemainingUses => DynamicVars[UsesVarName].IntValue;

    protected CGSLimitedUseCard(int baseCost, CardType type, CardRarity rarity, TargetType target,
        bool showInCardLibrary = true) : base(baseCost, type, rarity, target, showInCardLibrary)
    {
    }

    protected override PileType GetResultPileTypeForCardPlay()
    {
        var resultPileType = base.GetResultPileTypeForCardPlay();
        return RemainingUses == 1 ? Entry.CGSFatePile : resultPileType;
    }

    protected void ConsumeUse(CardPlay cardPlay)
    {
        var uses = DynamicVars[UsesVarName];
        uses.BaseValue = Math.Max(0m, uses.BaseValue - 1m);
    }

    public void RestoreUse()
    {
        DynamicVars[UsesVarName].BaseValue += 1m;
    }
}