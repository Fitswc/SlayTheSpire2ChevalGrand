using ChevalGrandSlay.Keywords;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

public abstract class CGSStaminaCardTemplate : ModCardTemplate
{
    protected CGSStaminaCardTemplate(int energyCost, CardType type, CardRarity rarity, TargetType targetType)
        : base(energyCost, type, rarity, targetType) { }

    protected CGSStaminaCardTemplate(int energyCost, CardType type, CardRarity rarity, TargetType targetType,
        bool shouldShowInCardLibrary)
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            foreach (var tip in base.AdditionalHoverTips)
                yield return tip;

            yield return HoverTipFactory.FromKeyword(CGSKeywords.Stamina);
        }
    }
}
