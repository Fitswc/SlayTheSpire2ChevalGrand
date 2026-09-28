using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Combat;
using ChevalGrandSlay.Mechanics;
using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Cards;

// 一往无前
[RegisterCard(typeof(CGSCardPool))]
public sealed class CGSForgeAhead : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    public CGSForgeAhead() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CGSForgeAheadPower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

[RegisterPower]
public sealed class CGSForgeAheadPower : ModPowerTemplate
{
    private int _triggeredTurn = -1;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new();

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner.Player;
        if (player?.PlayerCombatState is not { } state ||
            CombatState?.CurrentSide != CombatSide.Player ||
            cardPlay.Card.Owner != player ||
            cardPlay.Card.Type != CardType.Attack ||
            _triggeredTurn == state.TurnNumber ||
            CGSAttackChain.CountAttacksPlayedThisTurn(player, CombatState) < 3)
            return;

        _triggeredTurn = state.TurnNumber;
        Flash();
        await PlayerCmd.GainEnergy(1m, player);
    }
}
