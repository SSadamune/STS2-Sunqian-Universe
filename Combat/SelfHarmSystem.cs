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
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib;

namespace Squ.Combat;

public interface ISelfHarmCard
{
	decimal SelfHarmAmount { get; }
}

public static class SelfHarmSystem
{
	private sealed class State
	{
		public bool Played;
		public bool Triggered;
		public bool Prepared;
		public bool PayHp;
		public decimal Amount;
	}
	private static readonly ConditionalWeakTable<ICombatState, ConditionalWeakTable<CardModel, State>> Battles = new();
	public static void Initialize() => RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(evt =>
	{
		if (evt.CombatState != null) ClearCombat(evt.CombatState);
	});
	internal static void ClearCombat(ICombatState combat) => Battles.Remove(combat);
	private static State? GetState(CardModel card) => card.CombatState is { } combat
		? Battles.GetOrCreateValue(combat).GetOrCreateValue(card) : null;
	public static bool HasTriggered(CardModel card) => !card.IsCanonical
		&& GetState(card)?.Triggered == true;

	public static bool IsActive(CardModel card)
	{
		if (card is not ISelfHarmCard effect || card.IsCanonical || card.Owner == null
			|| !CombatManager.Instance.IsInProgress
			|| card.Owner.Creature.IsDead || GetState(card) is not { Played: false } state)
			return false;
		return state.Prepared ? state.PayHp : card.Owner.Creature.CurrentHp > effect.SelfHarmAmount;
	}

	internal static void PreparePayment(CardModel card)
	{
		if (card is not ISelfHarmCard effect || GetState(card) is not { Played: false } state) return;
		state.Prepared = false;
		state.PayHp = IsActive(card);
		state.Amount = effect.SelfHarmAmount;
		state.Prepared = true;
	}

	/// <summary>Called once at actual resolution, before the card's effects.</summary>
	public static async Task<int> ResolveFirstPlay(PlayerChoiceContext context, CardModel card, CardPlay play)
	{
		if (card is not ISelfHarmCard effect || GetState(card) is not { Played: false } state) return 0;
		bool payHp = IsActive(card);
		decimal amount = state.Prepared ? state.Amount : effect.SelfHarmAmount;
		state.Played = true;
		state.Triggered = payHp;
		state.Prepared = false;
		card.InvokeEnergyCostChanged();
		if (!payHp) return 0;
		var result = await CreatureCmd.Damage(context, card.Owner.Creature, amount,
			ValueProp.Unblockable | ValueProp.Unpowered, card, play);
		return result.Where(hit => hit.Receiver == card.Owner.Creature).Sum(hit => hit.UnblockedDamage);
	}
}
