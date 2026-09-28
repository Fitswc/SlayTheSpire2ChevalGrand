using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

//稳固步调

namespace ChevalGrandSlay.Cards;

[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSPrefectDefend : ModCardTemplate
{
    // 基础耗能。
    private const int BaseEnergyCost = 3;

    // 卡牌类型。
    private const CardType CardKind = CardType.Power;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Rare;

    // 目标类型（Self 表示自己）。
    private const TargetType CardTarget = TargetType.Self;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    public override bool GainsBlock => true;

    // 卡图资源。
    // 如果你按这行代码写，文件名就对应 ChevalGrandSlay/images/cards/ChevalGrandSlayReorganizeThePace.png。
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

    // 卡牌基础数值。
    // BlockVar 会绑定到本地化里的 {Block:diff()}，升级时文本会自动显示差值。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("StartTurnBlock", 12m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [

    ];

    public CGSPrefectDefend() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary) { }

    // 打出时的效果逻辑，这里是获得格挡。
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CGSPerfectDefendPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["StartTurnBlock"].BaseValue,
            Owner.Creature,
            this);


    }

    // 升级后的效果逻辑。
    protected override void OnUpgrade()
    {
        DynamicVars["StartTurnBlock"].UpgradeValueBy(3m);
        EnergyCost.UpgradeBy(-1);
    }
}

[RegisterPower]
public sealed class CGSPerfectDefendPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    private int _remainingBlockTurns = 2;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/test_power.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/test_power.png"
    );

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side == CombatSide.Player &&
            participants.Contains(Owner) &&
            _remainingBlockTurns > 0)
        {
            await CreatureCmd.GainBlock(
                Owner,
                Amount,
                ValueProp.Unpowered,
                null);

            _remainingBlockTurns--;
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy && _remainingBlockTurns == 0)
            await PowerCmd.Remove(this);
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || Owner.Player == null || !props.IsPoweredAttack() || result.BlockedDamage <= 0 || !result.WasFullyBlocked)
            return;

        if (Owner.Player is not { } player) return;
        await PlayerCmd.GainEnergy(1m, player);
        await CardPileCmd.Draw(choiceContext, 1m, player);
    }
}
