using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 终局冲线
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSFinalSprint : ModCardTemplate
{
    // 美术暂缺，使用框架默认资源。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Uses", 2m), new DynamicVar("MaxUses", 2m), new DamageVar(14m, ValueProp.Move), new DynamicVar("FateBonusCap", 8m)];

    public CGSFinalSprint() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);

        ArgumentNullException.ThrowIfNull(CombatState);
        int cardsInFate = Entry.CGSFatePile.GetPile(Owner).Cards.Count;
        decimal bonus = Math.Min(cardsInFate * 2m, DynamicVars["FateBonusCap"].BaseValue);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue + bonus).FromCard(this)
            .TargetingAllOpponents(CombatState).Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars["FateBonusCap"].UpgradeValueBy(2m);
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        DynamicVars["Uses"].IntValue <= 0 ? Entry.CGSFatePile : base.GetResultPileTypeForCardPlay();



}

