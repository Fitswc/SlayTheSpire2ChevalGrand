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
public sealed class ChevalGrandSlayDefenseStance : ModCardTemplate
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move)
    ];

    public ChevalGrandSlayDefenseStance() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self, true) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 进攻姿态
        await PowerCmd.Remove<ChevalGrandAttackStancePower>(Owner.Creature);

        // 进入防御姿态（已有则不重复刷新）
        if (!Owner.Creature.HasPower<ChevalGrandDefenseStancePower>())
        {
            await PowerCmd.Apply<ChevalGrandDefenseStancePower>(choiceContext, Owner.Creature, 0, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}