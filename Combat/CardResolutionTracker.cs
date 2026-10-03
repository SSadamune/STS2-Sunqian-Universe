using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Tracks nested <see cref="CardModel.OnPlayWrapper"/> calls per player. The first card in the
/// stack owns the complete resolution; cards auto-played from inside it remain child resolutions.
/// </summary>
public static class CardResolutionTracker
{
	private sealed class ResolutionScope
	{
		public required CardModel Card { get; init; }

		public required ulong PlayerId { get; init; }
	}

	private static readonly Dictionary<ulong, List<ResolutionScope>> ScopesByPlayer = [];
	private static readonly Dictionary<CardModel, int> AutoPlayDepthByCard = [];
	private static bool _initialized;

	public static void Initialize()
	{
		if (_initialized)
		{
			return;
		}

		_initialized = true;
		RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(_ => Clear());
	}

	public static bool IsOutermostResolution(CardModel card)
	{
		if (!ScopesByPlayer.TryGetValue(card.Owner.NetId, out List<ResolutionScope>? scopes)
			|| scopes.Count != 1)
		{
			return false;
		}

		return ReferenceEquals(scopes[0].Card, card);
	}

	public static bool IsOutermostCardPlay(CardPlay cardPlay) =>
		IsOutermostResolution(cardPlay.Card);

	public static bool IsBeingAutoPlayed(CardModel card) =>
		AutoPlayDepthByCard.TryGetValue(card, out int depth) && depth > 0;

	public static bool TryGetOutermostCard(Player? player, out CardModel card)
	{
		if (player is not null
			&& ScopesByPlayer.TryGetValue(player.NetId, out List<ResolutionScope>? scopes)
			&& scopes.Count > 0)
		{
			card = scopes[0].Card;
			return true;
		}

		card = null!;
		return false;
	}

	private static ResolutionScope BeginScope(CardModel card)
	{
		ulong playerId = card.Owner.NetId;
		var scope = new ResolutionScope
		{
			Card = card,
			PlayerId = playerId,
		};
		if (!ScopesByPlayer.TryGetValue(playerId, out List<ResolutionScope>? scopes))
		{
			scopes = [];
			ScopesByPlayer.Add(playerId, scopes);
		}

		scopes.Add(scope);
		return scope;
	}

	private static void BeginAutoPlay(CardModel card)
	{
		AutoPlayDepthByCard.TryGetValue(card, out int depth);
		AutoPlayDepthByCard[card] = depth + 1;
	}

	private static void EndAutoPlay(CardModel card)
	{
		if (!AutoPlayDepthByCard.TryGetValue(card, out int depth) || depth <= 1)
		{
			AutoPlayDepthByCard.Remove(card);
			return;
		}

		AutoPlayDepthByCard[card] = depth - 1;
	}

	private static void Clear()
	{
		ScopesByPlayer.Clear();
		AutoPlayDepthByCard.Clear();
	}

	private static void EndScope(ResolutionScope scope)
	{
		ulong playerId = scope.PlayerId;
		if (!ScopesByPlayer.TryGetValue(playerId, out List<ResolutionScope>? scopes))
		{
			return;
		}

		if (scopes.Count > 0 && ReferenceEquals(scopes[^1], scope))
		{
			scopes.RemoveAt(scopes.Count - 1);
		}
		else
		{
			scopes.Remove(scope);
		}

		if (scopes.Count == 0)
		{
			ScopesByPlayer.Remove(playerId);
		}
	}

	private static async Task EndScopeAfterAsync(
		Task playTask,
		ResolutionScope scope)
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

	private static async Task EndAutoPlayAfterAsync(Task playTask, CardModel card)
	{
		try
		{
			await playTask;
		}
		finally
		{
			EndAutoPlay(card);
		}
	}

	[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.AutoPlay))]
	private static class AutoPlayPatch
	{
		[HarmonyPriority(Priority.First)]
		private static void Prefix(CardModel card)
		{
			BeginAutoPlay(card);
		}

		private static void Postfix(CardModel card, ref Task __result)
		{
			__result = EndAutoPlayAfterAsync(__result, card);
		}
	}

	[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
	private static class OnPlayWrapperPatch
	{
		[HarmonyPriority(Priority.First)]
		private static void Prefix(CardModel __instance, out ResolutionScope __state)
		{
			__state = BeginScope(__instance);
		}

		private static void Postfix(
			CardModel __instance,
			ResolutionScope __state,
			ref Task __result)
		{
			__result = EndScopeAfterAsync(__result, __state);
		}
	}
}
