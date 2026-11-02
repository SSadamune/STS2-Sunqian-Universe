#nullable enable
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib;

namespace Squ.Combat;

public interface ISelfHarmCard
{
	decimal SelfHarmAmount { get; }
}

public static class SelfHarmSystem
{
	private enum SelfHarmKind { None, Native, Granted }
	private sealed class State
	{
		public SelfHarmKind Kind;
		public bool Prepared;
		public bool PayHp;
		public decimal Amount;
		public decimal? GrantedAmount;
	}
	private static readonly ConditionalWeakTable<ICombatState, ConditionalWeakTable<CardModel, State>> Battles = new();
	public static void Initialize() => RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(evt =>
	{
		if (evt.CombatState != null) ClearCombat(evt.CombatState);
	});
	internal static void ClearCombat(ICombatState combat) => Battles.Remove(combat);
	private static State? GetState(CardModel card) => !card.IsCanonical && card.CombatState is { } combat
		? Battles.GetOrCreateValue(combat).GetValue(card, current => new State
			{ Kind = current is ISelfHarmCard ? SelfHarmKind.Native : SelfHarmKind.None }) : null;

	public static decimal GetAmount(CardModel card) => GetState(card) is { Kind: SelfHarmKind.Granted } state
        ? state.GrantedAmount!.Value : card is ISelfHarmCard effect ? effect.SelfHarmAmount : 0;
    public static DynamicVar GetAmountVar(CardModel card) => GetState(card) is { Kind: SelfHarmKind.Granted } state
        ? new DynamicVar("SelfHarm", state.GrantedAmount!.Value) : card.DynamicVars["SelfHarm"];
    public static bool HasSelfHarm(CardModel card) => GetState(card) is { } state
        ? state.Kind != SelfHarmKind.None : card is ISelfHarmCard;
    public static void Grant(CardModel card, decimal amount)
    {
        if (HasSelfHarm(card) || GetState(card) is not { } state) return;
        state.GrantedAmount = Math.Max(0, amount);
        state.Kind = SelfHarmKind.Granted;
        state.Prepared = false;
        card.InvokeEnergyCostChanged();
    }

	public static bool IsActive(CardModel card)
	{
		if (card.IsCanonical || !HasSelfHarm(card) || card.Owner == null
			|| !CombatManager.Instance.IsInProgress
			|| card.Owner.Creature.IsDead || GetState(card) is not { } state)
			return false;
		return state.Prepared ? state.PayHp : card.Owner.Creature.CurrentHp > GetAmount(card);
	}

	internal static void PreparePayment(CardModel card)
	{
		if (!HasSelfHarm(card) || GetState(card) is not { } state) return;
		state.Prepared = false;
		state.PayHp = IsActive(card);
		state.Amount = GetAmount(card);
		state.Prepared = true;
	}

	/// <summary>Called once at actual resolution, before the card's effects.</summary>
	public static async Task<int> ResolvePayment(PlayerChoiceContext context, CardModel card, CardPlay play)
	{
		if (!HasSelfHarm(card) || GetState(card) is not { } state) return 0;
		bool payHp = IsActive(card);
		decimal amount = state.Prepared ? state.Amount : GetAmount(card);
		state.Prepared = false;
		if (!payHp) return 0;
		state.Kind = SelfHarmKind.None;
		card.InvokeEnergyCostChanged();
		var result = await CreatureCmd.Damage(context, card.Owner.Creature, amount,
			ValueProp.Unblockable | ValueProp.Unpowered, card, play);
		return result.Where(hit => hit.Receiver == card.Owner.Creature).Sum(hit => hit.UnblockedDamage);
	}
}
