using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卡牌：穿浪
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSPierceTheGap : CGSStaminaCard
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

    public CGSPierceTheGap() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CGSKeywords.Stamina];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new DynamicVar(CGSStamina.Uses, 6m),
            new DynamicVar(CGSStamina.MaxUses, 6m),
            ModCardVars.Damage(10m, ValueProp.Move | ValueProp.Unblockable)
        ];

    protected override async Task OnStaminaPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        
        VfxCmd.PlayOnCreatureCenter(cardPlay.Target, "vfx/vfx_attack_slash");
        await CreatureCmd.Damage(choiceContext, cardPlay.Target, DynamicVars.Damage.BaseValue,
            ValueProp.Move | ValueProp.Unblockable, Owner.Creature, this, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
