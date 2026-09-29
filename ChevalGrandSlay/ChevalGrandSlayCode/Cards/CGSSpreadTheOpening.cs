using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 扩散破绽
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSSpreadTheOpening : ModCardTemplate
{
    public override CardAssetProfile AssetProfile =>
        new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [
            new DynamicVar("Transfer", 1m)
        ];
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => 
        [
            CardKeyword.Exhaust
        ];

    public CGSSpreadTheOpening() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy, true)
    {
        
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        
        var vulnerable = cardPlay.Target.Powers.OfType<VulnerablePower>().FirstOrDefault();
        decimal amount = Math.Min(Math.Max(0m, vulnerable?.Amount ?? 0m), DynamicVars["Transfer"].BaseValue);
        if (amount == 0m || vulnerable is null)
        {
            return;
        }
        
        await PowerCmd.ModifyAmount(choiceContext, vulnerable, -amount, Owner.Creature, this);
        var combatState = CombatState ?? throw new InvalidOperationException("The card requires an active combat.");
        
        foreach (var enemy in combatState.HittableEnemies.Where(enemy => enemy != cardPlay.Target).ToArray())
            await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Transfer"].UpgradeValueBy(1m);
}