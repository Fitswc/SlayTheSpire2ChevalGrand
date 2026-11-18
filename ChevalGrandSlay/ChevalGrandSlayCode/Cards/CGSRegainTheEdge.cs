using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using ChevalGrandSlay.Utilities;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 卡牌：说我也可以
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSRegainTheEdge : CGSStaminaCard
{
    // X 费卡由 HasEnergyCostX 决定耗能。
    private const int BaseEnergyCost = 0;

    // 卡牌类型。
    private const CardType CardKind = CardType.Attack;

    // 卡牌稀有度。
    private const CardRarity CardRarityValue = CardRarity.Uncommon;

    // 目标类型（AllEnemies 表示所有敌人）。
    private const TargetType CardTarget = TargetType.AllEnemies;

    // 是否在卡牌图鉴中显示。
    private const bool ShowInCardLibrary = true;

    public override int MaxUpgradeLevel => 0;

    public CGSRegainTheEdge() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CGSKeywords.Stamina];
    protected override bool HasEnergyCostX => true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(CGSStamina.Uses, 6m),
        new DynamicVar(CGSStamina.MaxUses, 6m),
        new DynamicVar("DamagePerStack", 10m)
    ];

    protected override async Task OnStaminaPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState is not { } combatState)
            return;

        var creature = Owner.Creature;
        int cleanseLimit = ResolveEnergyXValue();
        int removedStacks = 0;
        for (int i = 0; i < cleanseLimit; i++)
        {
            var debuff = creature.Powers.FirstOrDefault(power => power.Type == PowerType.Debuff && power.Amount > 0);
            if (debuff is null)
                break;

            int previousAmount = debuff.Amount;
            await PowerCmd.ModifyAmount(choiceContext, debuff, -1m, creature, this);
            removedStacks += Math.Max(0, previousAmount - Math.Max(0, debuff.Amount));
        }

        if (removedStacks == 0)
            return;

        decimal damage = DynamicVars["DamagePerStack"].BaseValue * removedStacks;
        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay).CGSWithVfx("vfx/vfx_attack_slash")
            .TargetingAllOpponents(combatState).Execute(choiceContext);
    }
}
