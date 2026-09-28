using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

// 冰点

namespace ChevalGrandSlay.Cards;

[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSSubZero : ModCardTemplate
{
    // 基础耗能。
    private const int BaseEnergyCost = 2;

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
        new BlockVar(12m, ValueProp.Unpowered)
    ];

    public CGSSubZero() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary) { }

    // 先弃牌，再获得持续三个己方回合的格挡效果。
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selectedCards = await CardSelectCmd.FromHandForDiscard(
            choiceContext,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1),
            null,
            this
        );

        var selectedCard = selectedCards.FirstOrDefault();

        if (selectedCard != null)
        {
            await CardCmd.Discard(choiceContext, selectedCard);
        }

        await PowerCmd.Apply<CGSSubZeroPower>(
            choiceContext, Owner.Creature, DynamicVars.Block.BaseValue, Owner.Creature, this);
    }

    // 升级后的效果逻辑。
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
        EnergyCost.UpgradeBy(-1);
    }
}

[RegisterPower]
public sealed class CGSSubZeroPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    // 每次打出独立计时，避免改变已有冰点的持续时间。
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Turns", 3m)
    ];

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/test_power.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/test_power.png"
    );

    public override async Task BeforeSideTurnEndEarly(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner) || DynamicVars["Turns"].IntValue <= 0)
            return;

        Flash();
        await CreatureCmd.GainBlock(
            Owner, Amount, ValueProp.Unpowered, null);

        DynamicVars["Turns"].BaseValue -= 1m;
        if (DynamicVars["Turns"].IntValue == 0)
            await PowerCmd.Remove(this);
    }
}
