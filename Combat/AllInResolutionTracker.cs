#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using Squ.Powers;
using STS2RitsuLib;

namespace Squ.Combat;

/// <summary>
/// Tracks the exact lifetime of a card play empowered by All In. A manual play qualifies when its
/// resolved energy cost before payment is positive and equals the player's energy before payment.
/// The scope wraps the complete vanilla <see cref="CardModel.OnPlayWrapper"/>, so nested auto-plays
/// inherit the bonus without extending it beyond the original card's resolution.
/// </summary>
public static class AllInResolutionTracker
{
	private sealed class SpendSnapshot
	{
		public int EnergyBeforePayment;
		public int EnergyCostBeforePayment;
		public bool Pending;
	}

	private sealed class ResolutionScope
	{
		public required Player Player { get; init; }
		public required decimal BonusPercent { get; init; }
	}

	private static readonly ConditionalWeakTable<CardModel, SpendSnapshot> SpendSnapshots = new();
	private static readonly Dictionary<ulong, Stack<ResolutionScope>> ActiveScopes = [];
	[ThreadStatic]
	private static int _cardPreviewDepth;
	private static bool _initialized;

	public static bool IsUpdatingCardPreview => _cardPreviewDepth > 0;

	public static void Initialize()
	{
		if (_initialized)
		{
			return;
		}

		_initialized = true;
		RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(_ => Clear());
	}

	/// <summary>Returns the percentage captured when the currently resolving root card qualified.</summary>
	public static bool TryGetActiveBonus(Player? player, out decimal bonusPercent)
	{
		if (player is not null
			&& ActiveScopes.TryGetValue(player.NetId, out Stack<ResolutionScope>? scopes)
			&& scopes.TryPeek(out ResolutionScope? scope))
		{
			bonusPercent = scope.BonusPercent;
			return true;
		}

		bonusPercent = 0m;
		return false;
	}

	/// <summary>Used by hand previews; auto-played cards never call this path.</summary>
	public static bool WillConsumeLastEnergy(CardModel? card)
	{
		if (card?.Owner?.PlayerCombatState is not { Energy: > 0 } combatState
			|| card.Pile?.Type != PileType.Hand)
		{
			return false;
		}

		int cost = card.EnergyCost.GetAmountToSpend();
		return cost > 0 && cost == combatState.Energy;
	}

	private static void Clear()
	{
		SpendSnapshots.Clear();
		ActiveScopes.Clear();
	}

	private static void CaptureSpendSnapshot(CardModel card)
	{
		SpendSnapshot snapshot = SpendSnapshots.GetOrCreateValue(card);
		if (card.Owner.PlayerCombatState is not { } combatState)
		{
			snapshot.Pending = false;
			return;
		}

		snapshot.EnergyBeforePayment = combatState.Energy;
		snapshot.EnergyCostBeforePayment = card.EnergyCost.GetAmountToSpend();
		snapshot.Pending = true;
	}

	private static bool TryConsumeSpendSnapshot(CardModel card, out SpendSnapshot snapshot)
	{
		snapshot = SpendSnapshots.GetOrCreateValue(card);
		if (!snapshot.Pending)
		{
			return false;
		}

		snapshot.Pending = false;
		return true;
	}

	private static ResolutionScope? BeginScope(
		CardModel card,
		bool isAutoPlay,
		ResourceInfo resources)
	{
		if (isAutoPlay
			|| resources.EnergySpent <= 0
			|| TryGetActiveBonus(card.Owner, out _)
			|| !TryConsumeSpendSnapshot(card, out SpendSnapshot snapshot)
			|| snapshot.EnergyCostBeforePayment <= 0
			|| snapshot.EnergyCostBeforePayment != snapshot.EnergyBeforePayment
			|| card.Owner.Creature.GetPower<AllInPower>() is not { Amount: > 0 } power)
		{
			return null;
		}

		var scope = new ResolutionScope
		{
			Player = card.Owner,
			BonusPercent = power.Amount,
		};

		if (!ActiveScopes.TryGetValue(scope.Player.NetId, out Stack<ResolutionScope>? scopes))
		{
			scopes = new Stack<ResolutionScope>();
			ActiveScopes.Add(scope.Player.NetId, scopes);
		}

		scopes.Push(scope);
		return scope;
	}

	private static void EndScope(ResolutionScope scope)
	{
		if (!ActiveScopes.TryGetValue(scope.Player.NetId, out Stack<ResolutionScope>? scopes))
		{
			return;
		}

		if (scopes.Count > 0 && ReferenceEquals(scopes.Peek(), scope))
		{
			scopes.Pop();
		}

		if (scopes.Count == 0)
		{
			ActiveScopes.Remove(scope.Player.NetId);
		}
	}

	private static async Task EndScopeAfterAsync(Task playTask, ResolutionScope scope)
	{
		try
		{
			await playTask;
		}
		finally
		{
			EndScope(scope);
		}
	}

	[HarmonyPatch(typeof(CardModel), nameof(CardModel.SpendResources))]
	private static class SpendResourcesPatch
	{
		private static void Prefix(CardModel __instance) => CaptureSpendSnapshot(__instance);
	}

	[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
	private static class OnPlayWrapperPatch
	{
		private static void Prefix(
			CardModel __instance,
			bool isAutoPlay,
			ResourceInfo resources,
			out ResolutionScope? __state)
		{
			__state = BeginScope(__instance, isAutoPlay, resources);
		}

		private static void Postfix(ref Task __result, ResolutionScope? __state)
		{
			if (__state is not null)
			{
				__result = EndScopeAfterAsync(__result, __state);
			}
		}
	}

	[HarmonyPatch(typeof(CardModel), nameof(CardModel.UpdateDynamicVarPreview))]
	private static class UpdateDynamicVarPreviewPatch
	{
		private static void Prefix() => _cardPreviewDepth++;

		private static void Postfix() => _cardPreviewDepth--;
	}
}
