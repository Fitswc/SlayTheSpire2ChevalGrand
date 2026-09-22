using ChevalGrandSlay.CardPiles;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

public abstract class CGSLimitedUseCard : ModCardTemplate
{
    public const string UsesVarName = "Uses";

    protected CGSLimitedUseCard(int baseCost, CardType type, CardRarity rarity, TargetType target)
        : base(baseCost, type, rarity, target, true)
    {
        this.AddCapability(new CGSFatePileResultCapability());
    }

    public int RemainingUses => DynamicVars[UsesVarName].IntValue;

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