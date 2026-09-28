using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 进退有界
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSMeasuredAdvanceAndRetreat : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new DynamicVar("Uses", 2m), new DynamicVar("MaxUses", 2m), 
            new DynamicVar("LossCap", 12m)
        ];
    
    public CGSMeasuredAdvanceAndRetreat() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self, true)
    {
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ConsumeUse();
        await PowerCmd.Apply<CGSEnemyAttackLossCapPower>(choiceContext, Owner.Creature,
            DynamicVars["LossCap"].BaseValue, Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        DynamicVars["LossCap"].UpgradeValueBy(-4m);
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

[RegisterPower]
public sealed class CGSEnemyAttackLossCapPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => new();
    private decimal _lost;

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal hpLoss, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || dealer?.IsEnemy != true || !props.IsPoweredAttack() ||
            CombatState?.CurrentSide != CombatSide.Enemy) return hpLoss;
        decimal allowed = Math.Max(0m, Amount - _lost);
        decimal actual = Math.Min(hpLoss, allowed);
        _lost += actual;
        return actual;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy) await PowerCmd.Remove(this);
    }
}
