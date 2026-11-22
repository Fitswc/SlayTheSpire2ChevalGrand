using global::AutoAnthony;
using ChevalGrandSlay.Characters;
using ChevalGrandSlay.Keywords;
using ChevalGrandSlay.Mechanics;
using ChevalGrandSlay.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace ChevalGrandSlay.Compatibility.AutoAnthony;

public abstract class ChevalChaosCard : ExternalChaosCardModel, ICGSGeneratedCard
{
    private const int StartingStamina = 4;
    private int _discountedTurn = -1;
    private int _restEnteredTurn = -1;
    private int _retainedTurn = -1;
    private string? _cachedHistoryPayload;
    private ChaosCardDefinition? _cachedHistoryDefinition;
    internal bool IsResolving { get; private set; }

    internal bool RestEnteredThisTurn => Owner?.PlayerCombatState?.TurnNumber == _restEnteredTurn;
    internal bool RetainedLastTurn => _retainedTurn >= 0 && Owner?.PlayerCombatState is { } state &&
        _retainedTurn == state.TurnNumber - 1;

    internal bool CanUseFinalKick => Owner?.PlayerCombatState is { Energy: 0 } &&
        Entry.CGSFatePile.GetPile(Owner).Cards.Count > 0 &&
        !PileType.Draw.GetPile(Owner).Cards.Any(CGSStamina.HasStamina) &&
        !PileType.Discard.GetPile(Owner).Cards.Any(CGSStamina.HasStamina);

    protected override sealed string ComponentProfileId => ChevalComponents.ProfileId;
    public override sealed CardPoolModel Pool => ModelDb.CardPool<CGSCardPool>();

    protected override sealed ChaosCardDefinition ResolveDefinition()
    {
        if (CGSStamina.IsEmptyCopy(this))
            return ChevalHistory.Placeholder(Slot);

        if (ChevalRun.IsActive)
            return base.ResolveDefinition();

        var saved = ChevalHistory.Read(this);
        if (!string.Equals(saved, _cachedHistoryPayload, StringComparison.Ordinal))
        {
            _cachedHistoryPayload = saved;
            _cachedHistoryDefinition = null;
            if (!string.IsNullOrEmpty(saved))
            {
                try
                {
                    _cachedHistoryDefinition = ChevalRun.DecodeCard(saved, Slot);
                }
                catch (Exception error)
                {
                    Entry.Logger.Warn($"[AutoAnthony] Could not restore a history card: {error.GetBaseException().Message}");
                }
            }
        }
        return _cachedHistoryDefinition ?? ChevalHistory.Placeholder(Slot);
    }

    internal void RefreshRestoredState()
    {
        base.AfterDeserialized();
        CGSStamina.RestoreDeckCard(this);
    }

    protected override sealed IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Concat([
            new DynamicVar(CGSStamina.Uses, StartingStamina),
            new DynamicVar(CGSStamina.MaxUses, StartingStamina)
        ]);

    public override sealed IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CGSKeywords.Stamina;
            var generated = base.CanonicalKeywords;
            if (HasEffect(ChevalComponents.RetainedDamage) || HasEffect(ChevalComponents.FinalKick))
                generated = generated.Append(CardKeyword.Retain);
            foreach (var keyword in generated.Distinct())
                yield return keyword;
        }
    }

    protected override bool IsPlayable => ChevalRun.IsActive && !IsResolving && base.IsPlayable &&
        (!HasEffect(ChevalComponents.FinalKick) || CanUseFinalKick) &&
        (!HasEffect(ChevalComponents.Domain) || !Owner.Creature.HasPower<CGSDomainPower>());

    internal bool HasEffect(string variant) => ChevalComponents.HasEffect(ResolveDefinition().Card, variant);

    internal int GetEnergyX(CardPlay cardPlay) => CombatState is { } combat
        ? Hook.ModifyXValue(combat, this, cardPlay.Resources.EnergySpent > 0
            ? cardPlay.Resources.EnergySpent : EnergyCost.CapturedXValue)
        : 0;

    protected override async sealed Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CGSStamina.IsEmptyCopy(this))
            return;

        IsResolving = true;
        try
        {
            CGSConsumeUse.Consume(this);
            await base.OnPlay(choiceContext, cardPlay);
        }
        finally
        {
            IsResolving = false;
        }
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        RefreshDiscount(cardPlay);
        return base.BeforeCardPlayed(cardPlay);
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RefreshDiscount();
        return base.AfterCardPlayed(choiceContext, cardPlay);
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType previousPile, AbstractModel? source)
    {
        if (Owner?.PlayerCombatState is { } state && card.Owner == Owner &&
            card.Pile?.Type == Entry.CGSFatePile && previousPile != Entry.CGSFatePile)
            _restEnteredTurn = state.TurnNumber;
        return base.AfterCardChangedPiles(card, previousPile, source);
    }

    public override Task AfterFlush(PlayerChoiceContext choiceContext,
        Player player, IReadOnlyCollection<CardModel> flushedCards,
        IReadOnlyCollection<CardModel> retainedCards)
    {
        if (player == Owner && retainedCards.Contains(this) && player.PlayerCombatState is { } state)
            _retainedTurn = state.TurnNumber;
        return base.AfterFlush(choiceContext, player, flushedCards, retainedCards);
    }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _discountedTurn = _restEnteredTurn = _retainedTurn = -1;
        IsResolving = false;
    }

    private void RefreshDiscount(CardPlay? excludedPlay = null)
    {
        if (Owner?.PlayerCombatState is not { } state || _discountedTurn == state.TurnNumber ||
            !HasEffect(ChevalComponents.ChainDiscount) || EnergyCost.CostsX ||
            CGSAttackChain.CountAttacksPlayedThisTurn(Owner, CombatState, excludedPlay) < 2)
            return;
        EnergyCost.AddThisTurn(-1);
        _discountedTurn = state.TurnNumber;
    }
}
