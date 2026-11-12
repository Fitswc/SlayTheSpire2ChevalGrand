using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Utilities;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.HandSize;
using STS2RitsuLib.Combat.HealthBars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

// 能力：领域
[RegisterPower]
public sealed class CGSDomainPower : ModPowerTemplate, IHealthBarForecastSource
{
    private int _appliedTurnNumber;
    private bool _hasReachedNextTurn;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromKeyword(CGSKeywords.RestPile)
    ];

    public const decimal DamageBonusPercent = 25m;
    public const int EnergyPerTurn = 4;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    // 自定义图标路径。1:1即可。原版游戏大图256x256，小图64x64。
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/CGSDomainPower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/CGSDomainPower.png"
    );

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner.Player is not { } player)
            return;

        _appliedTurnNumber = player.PlayerCombatState?.TurnNumber ?? 0;
        _hasReachedNextTurn = false;

        await PlayerCmd.GainEnergy(EnergyPerTurn, player);
        CGSDomainBGGray.FadeBackgroundToGrayscale(1.8);
        CGSDomainAura.Show(Owner);

        // Snapshot the pile because moving cards can trigger other powers and change its contents.
        var restingCards = Entry.CGSFatePile.GetPile(player).Cards.ToArray();
        foreach (var card in restingCards)
        {
            if (card.Pile?.Type != Entry.CGSFatePile)
                continue;

            if (card.DynamicVars.TryGetValue("Uses", out var uses) &&
                card.DynamicVars.TryGetValue("MaxUses", out var maxUses))
                uses.BaseValue = maxUses.BaseValue;

            var destination = PileType.Hand.GetPile(player).Cards.Count < MaxHandSizeCalculator.Calculate(player)
                ? PileType.Hand
                : PileType.Draw;
            await CardPileCmd.Add(card, destination);
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        CGSDomainBGGray.RestoreBackgroundColor();
        CGSDomainAura.Remove(Owner);
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource) =>
        dealer == Owner && cardSource?.Type == CardType.Attack && props.IsPoweredAttack()
            ? 1m + DamageBonusPercent / 100m
            : 1m;

    public override async Task AfterEnergyReset(Player player)
    {
        if (player == Owner.Player)
        {
            if (player.PlayerCombatState?.TurnNumber > _appliedTurnNumber)
                _hasReachedNextTurn = true;

            await PlayerCmd.GainEnergy(EnergyPerTurn, player);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner) || Owner.IsDead ||
            CombatManager.Instance.IsOverOrEnding)
            return;

        if (Amount > 1m)
            await PowerCmd.Decrement(this);
        else
            await CreatureCmd.Kill(Owner, force: true);
    }

    public IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context)
    {

        int currentHp = context.Creature.CurrentHp;
        
        int coveredHp = _hasReachedNextTurn ? currentHp : currentHp / 2;
        return HealthBarForecasts.Single(
            coveredHp,
            new Color(0.4f, 0.1f, 0.1f),
            HealthBarForecastGrowthDirection.FromLeft
        );
    }
}
