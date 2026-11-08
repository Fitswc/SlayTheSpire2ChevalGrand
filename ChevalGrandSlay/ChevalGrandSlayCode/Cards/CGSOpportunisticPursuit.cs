using ChevalGrandSlay.Utilities;
using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卡牌：追近那个人
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSOpportunisticPursuit : ModCardTemplate
{
    // 基础耗能。
    private const int BaseEnergyCost = 1;

    // 卡牌类型。
    private const CardType CardKind = CardType.Attack;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Common;

    // 目标类型（AnyEnemy 表示任意敌人）。
    private const TargetType CardTarget = TargetType.AnyEnemy;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CGSKeywords.Stamina];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Uses", 3m), 
        new DynamicVar("MaxUses", 3m),
        new DamageVar(6m, ValueProp.Move),
        new DynamicVar("BonusDamage", 5m),
    ];

    public CGSOpportunisticPursuit() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CGSConsumeUse.Consume(this);
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        
        bool hasPreviousAttack = CGSAttackChain.CountAttacksPlayedThisTurn(Owner, CombatState, cardPlay) >= 1;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this).CGSWithVfx("vfx/vfx_attack_slash").Targeting(cardPlay.Target).Execute(choiceContext);

        if (hasPreviousAttack)
        {
            await DamageCmd.Attack(DynamicVars["BonusDamage"].BaseValue)
                .FromCard(this).CGSWithVfx("vfx/vfx_attack_slash").Targeting(cardPlay.Target).Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Uses"].UpgradeValueBy(1m);
        DynamicVars["MaxUses"].UpgradeValueBy(1m);
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["BonusDamage"].UpgradeValueBy(2m);
    }

    protected override PileType GetResultPileTypeForCardPlay() =>
        CGSConsumeUse.GetResultPileTypeForCardPlay(this, base.GetResultPileTypeForCardPlay());

}
