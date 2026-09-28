using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 倾注一式
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSPourIntoOneMove : ModCardTemplate
{
    public override CardAssetProfile AssetProfile =>
        new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Uses", 2m), new DynamicVar("MaxUses", 2m)];

    protected override bool IsPlayable => base.IsPlayable && Owner is not null &&
        PileType.Hand.GetPile(Owner).Cards.Any(IsCandidate);

    public CGSPourIntoOneMove() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);
        var selected = await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), IsCandidate, this);
        if (selected.FirstOrDefault() is not { } attack) return;
        var cost = attack.EnergyCost.GetWithModifiers(CostModifiers.All);
        await PlayerCmd.LoseEnergy(cost, Owner);
        attack.DynamicVars["Uses"].BaseValue -= 1m;
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var power = await PowerCmd.Apply<CGSFirstStrikeTwicePower>(choiceContext, Owner.Creature,
            1m, Owner.Creature, this);
        power?.SetAttack(attack);
        try
        {
            await CardCmd.AutoPlay(choiceContext, attack, cardPlay.Target);
        }
        finally
        {
            if (power is not null)
                await PowerCmd.Remove(power);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
    }

    private bool IsCandidate(CardModel card) =>
        card.Type == CardType.Attack && card.DynamicVars.ContainsKey("Uses") &&
        card.DynamicVars["Uses"].IntValue >= 2 &&
        card.EnergyCost.GetWithModifiers(CostModifiers.All) <= (Owner?.PlayerCombatState?.Energy ?? 0);

    protected override PileType GetResultPileTypeForCardPlay() =>
        DynamicVars["Uses"].IntValue <= 0 ? Entry.CGSFatePile : base.GetResultPileTypeForCardPlay();

}
