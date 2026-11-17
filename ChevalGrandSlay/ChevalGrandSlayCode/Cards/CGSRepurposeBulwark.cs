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

// 卡牌：借来的从容
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSRepurposeBulwark : CGSStaminaCard
{
    // 基础耗能。
    private const int BaseEnergyCost = 1;

    // 卡牌类型。
    private const CardType CardKind = CardType.Skill;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Uncommon;

    // 目标类型（AnyEnemy 表示任意敌人）。
    private const TargetType CardTarget = TargetType.AnyEnemy;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    public CGSRepurposeBulwark() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CGSKeywords.Stamina];

    public override bool GainsBlock => true;
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(CGSStamina.Uses, 4m),
        new DynamicVar(CGSStamina.MaxUses, 4m),
        new DynamicVar("BlockLimit", 20m)
    ];

    protected override async Task OnStaminaPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        var blockToRemove = Math.Min(cardPlay.Target.Block, DynamicVars["BlockLimit"].IntValue);
        if (blockToRemove > 0)
        {
            await CreatureCmd.LoseBlock(choiceContext, cardPlay.Target, blockToRemove, null);
            await CreatureCmd.GainBlock(Owner.Creature, blockToRemove, ValueProp.Move, cardPlay);
        }
        else
        {
            await CreatureCmd.Damage(choiceContext, cardPlay.Target, 12m, ValueProp.Unblockable, Owner.Creature, this, cardPlay);
        }

    }

    protected override void OnUpgrade()
    {
        DynamicVars["BlockLimit"].UpgradeValueBy(4m);
    }
}
