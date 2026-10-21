using ChevalGrandSlay.Keywords;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.HandSize;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSDomainPower : ModPowerTemplate
{
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

        await PlayerCmd.GainEnergy(EnergyPerTurn, player);

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

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource) =>
        dealer == Owner && cardSource?.Type == CardType.Attack && props.IsPoweredAttack()
            ? 1m + DamageBonusPercent / 100m
            : 1m;

    public override async Task AfterEnergyReset(Player player)
    {
        if (player == Owner.Player)
            await PlayerCmd.GainEnergy(EnergyPerTurn, player);
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
}
