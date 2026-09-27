using MegaCrit.Sts2.Core.Entities.Cards;
using ChevalGrandSlay.Powers;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Mechanics;

public abstract class CGSLimitedUseCard : ModCardTemplate
{
    public const string UsesVarName = "Uses";

    public int RemainingUses => DynamicVars[UsesVarName].IntValue;
    public int MaximumUses => CanonicalInstance.DynamicVars[UsesVarName].IntValue;

    protected CGSLimitedUseCard(int baseCost, CardType type, CardRarity rarity, TargetType target,
        bool showInCardLibrary = true) : base(baseCost, type, rarity, target, showInCardLibrary)
    {
    }

    protected override PileType GetResultPileTypeForCardPlay()
    {
        var resultPileType = base.GetResultPileTypeForCardPlay();
        return RemainingUses <= 1 ? PileType.Exhaust : resultPileType;
    }

    protected void ConsumeUse(CardPlay cardPlay)
    {
        if (Owner.Creature.Powers.OfType<CGSOldPactPower>().Any(power => power.TryPreserveUse(cardPlay)))
            return;
        var uses = DynamicVars[UsesVarName];
        uses.BaseValue = Math.Max(0m, uses.BaseValue - 1m);
    }

    public void RestoreUse()
    {
        RestoreUses(1);
    }

    public void ConsumeAllUses()
    {
        DynamicVars[UsesVarName].BaseValue = 0m;
    }

    public void SpendUses(int amount)
    {
        var uses = DynamicVars[UsesVarName];
        uses.BaseValue = Math.Max(0m, uses.BaseValue - Math.Max(0, amount));
    }

    public void RestoreUses(int amount)
    {
        var uses = DynamicVars[UsesVarName];
        uses.BaseValue = Math.Min(MaximumUses, uses.BaseValue + Math.Max(0, amount));
    }
}
