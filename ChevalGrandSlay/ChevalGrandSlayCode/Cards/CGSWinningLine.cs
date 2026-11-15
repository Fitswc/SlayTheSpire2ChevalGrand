using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using ChevalGrandSlay.Utilities;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卡牌：伸手可及
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSWinningLine : CGSStaminaCard
{
    // 基础耗能。
    private const int BaseEnergyCost = 2;

    // 卡牌类型。
    private const CardType CardKind = CardType.Attack;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Rare;

    // 目标类型（AnyEnemy 表示任意敌人）。
    private const TargetType CardTarget = TargetType.AnyEnemy;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    private const string BonusDamageVarName = "BonusDamage";

    public CGSWinningLine() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CGSKeywords.Stamina];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move),
        new DynamicVar(BonusDamageVarName, 4m),
        new DynamicVar(CGSStamina.Uses, 3m),
        new DynamicVar(CGSStamina.MaxUses, 3m)
    ];

    protected override async Task OnStaminaPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        
        
        int previousAttacks = Math.Min(4, CGSAttackChain.CountAttacksPlayedThisTurn(Owner, CombatState, cardPlay));
        decimal damage = DynamicVars.Damage.BaseValue +
                         previousAttacks * DynamicVars[BonusDamageVarName].BaseValue;

        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay).CGSWithVfx("vfx/vfx_attack_slash")
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        UpgradeStamina(1m);
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars[BonusDamageVarName].UpgradeValueBy(1m);
        EnergyCost.UpgradeBy(-1);
    }
}