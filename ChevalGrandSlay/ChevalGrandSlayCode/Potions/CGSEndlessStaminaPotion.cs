using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Potions;

[RegisterPotion(typeof(CGSPotionPool))]
public sealed class CGSEndlessStaminaPotion : ModPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    // Reuse the game's potion art until dedicated artwork is available.
    public override PotionAssetProfile AssetProfile => new(
        ImagePath: "res://images/potions/CGSEndlessStaminaPotion.png",
        OutlinePath: "res://images/potions/CGSEndlessStaminaPotion.png");

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        await PowerCmd.Apply<CGSEndlessStaminaPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null);
    }
}
