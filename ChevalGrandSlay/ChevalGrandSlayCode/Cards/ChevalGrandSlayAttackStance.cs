using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

[RegisterCard(typeof(ChevalGrandSlayCardPool))]
[RegisterCharacterStarterCard(typeof(ChevalGrandSlayCharacter), 1)]
public sealed class ChevalGrandSlayAttackStance : ModCardTemplate
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8, ValueProp.Move)
    ];

    public ChevalGrandSlayAttackStance() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 1. 造成基础伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        // 2. 检查防御姿态的蓄力转化
        var defensePower = Owner.Creature.GetPower<ChevalGrandDefenseStancePower>();
        if (defensePower != null)
        {
            int storedBlockDmg = defensePower.Amount;
            if (storedBlockDmg > 0)
            {
                await DamageCmd.Attack(storedBlockDmg)
                    .FromCard(this)
                    .Targeting(cardPlay.Target)
                    .Execute(choiceContext);
            }
            await PowerCmd.Remove(defensePower);
        }

        // 3. 进入进攻姿态
        if (!Owner.Creature.HasPower<ChevalGrandAttackStancePower>())
        {
            await PowerCmd.Apply<ChevalGrandAttackStancePower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}