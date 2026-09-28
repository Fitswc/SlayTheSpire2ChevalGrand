using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 连踏
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSChainStep : ModCardTemplate
{
    private int _discountedTurn = -1;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Uses", 3m), new DynamicVar("MaxUses", 3m), new DamageVar(7m, ValueProp.Move)];

    public CGSChainStep() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ConsumeUse();
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this).Targeting(cardPlay.Target).Execute(choiceContext);
        if (DynamicVars["Uses"].IntValue == 0)
            await CardPileCmd.Draw(choiceContext, 1m, Owner);
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        RefreshDiscount(cardPlay);
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RefreshDiscount();
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
            _discountedTurn = -1;
        return Task.CompletedTask;
    }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _discountedTurn = -1;
    }

    private void RefreshDiscount(CardPlay? excludedPlay = null)
    {
        if (Owner?.PlayerCombatState is not { } state || _discountedTurn == state.TurnNumber ||
            CGSAttackChain.CountAttacksPlayedThisTurn(Owner, CombatState, excludedPlay) < 2)
            return;

        EnergyCost.AddThisTurn(-1);
        _discountedTurn = state.TurnNumber;
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        DynamicVars.Damage.UpgradeValueBy(3m);
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        DynamicVars["Uses"].IntValue <= 0 ? Entry.CGSFatePile : base.GetResultPileTypeForCardPlay();

    private void ConsumeUse()
    {
        if (Owner.Creature.Powers.OfType<CGSReserveAStepPower>().Any(power => power.TryPreserve(this)))
            return;
        var uses = DynamicVars["Uses"];
        uses.BaseValue = Math.Max(0m, uses.BaseValue - 1m);
    }
}
