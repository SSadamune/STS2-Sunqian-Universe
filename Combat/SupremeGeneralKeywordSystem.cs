using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Implements the Supreme General keyword. The outermost manually played card owns the window;
/// nested auto-plays can receive its spent Vigor, but cannot replace or amplify that window.
/// </summary>
[RegisterSingleton]
public sealed class SupremeGeneralKeywordSystem : HookedSingletonModel
{
	private sealed class ResolutionWindow
	{
		public required CardPlay RootPlay { get; init; }

		public required CardModel RootCard { get; init; }

		public decimal ConsumedVigor { get; set; }

		public Dictionary<AttackCommand, decimal> VigorBeforeAttack { get; } = [];
	}

	private static readonly Dictionary<ulong, List<ResolutionWindow>> WindowsByPlayer = [];

	public SupremeGeneralKeywordSystem()
		: base(HookType.Combat)
	{
	}

	public override Task BeforeCombatStart()
	{
		WindowsByPlayer.Clear();
		return Task.CompletedTask;
	}

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		if (cardPlay.IsAutoPlay || !cardPlay.Card.HasModKeyword(SquKeywords.SupremeGeneral))
		{
			return Task.CompletedTask;
		}

		ulong playerId = cardPlay.Player.NetId;
		if (!WindowsByPlayer.TryGetValue(playerId, out List<ResolutionWindow>? windows))
		{
			windows = [];
			WindowsByPlayer.Add(playerId, windows);
		}

		windows.Add(new ResolutionWindow
		{
			RootPlay = cardPlay,
			RootCard = cardPlay.Card,
		});

		return Task.CompletedTask;
	}

	public override Task BeforeAttack(AttackCommand command)
	{
		if (command.Attacker?.Player is not { } player
			|| !command.DamageProps.IsPoweredAttack()
			|| !TryGetOutermostWindow(player.NetId, out ResolutionWindow window)
			|| !ReferenceEquals(command.CardPlay, window.RootPlay))
		{
			return Task.CompletedTask;
		}

		window.VigorBeforeAttack[command] = GetVigorAmount(command.Attacker);
		return Task.CompletedTask;
	}

	public override Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
	{
		if (command.Attacker is not { } attacker
			|| attacker.Player is not { } player
			|| !TryGetOutermostWindow(player.NetId, out ResolutionWindow window)
			|| !window.VigorBeforeAttack.Remove(command, out decimal vigorBefore))
		{
			return Task.CompletedTask;
		}

		decimal consumed = vigorBefore - GetVigorAmount(attacker);
		if (consumed > 0m)
		{
			window.ConsumedVigor += consumed;
		}

		return Task.CompletedTask;
	}

	public override decimal ModifyDamageAdditive(
		Creature? target,
		decimal amount,
		ValueProp props,
		Creature? dealer,
		CardModel? cardSource,
		CardPlay? cardPlay)
	{
		if (cardPlay is null
			|| !cardPlay.IsAutoPlay
			|| cardSource is null
			|| cardSource.Type != CardType.Attack
			|| !props.IsPoweredAttack()
			|| !TryGetOutermostWindow(cardPlay.Player.NetId, out ResolutionWindow window)
			|| window.ConsumedVigor <= 0m
			|| ReferenceEquals(cardSource, window.RootCard)
			|| dealer != window.RootCard.Owner.Creature)
		{
			return 0m;
		}

		return window.ConsumedVigor;
	}

	public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (!cardPlay.IsAutoPlay)
		{
			CloseWindow(cardPlay);
		}

		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		WindowsByPlayer.Clear();
		return Task.CompletedTask;
	}

	private static decimal GetVigorAmount(Creature creature) =>
		creature.GetPower<VigorPower>()?.Amount ?? 0m;

	private static bool TryGetOutermostWindow(
		ulong playerId,
		out ResolutionWindow window)
	{
		if (WindowsByPlayer.TryGetValue(playerId, out List<ResolutionWindow>? windows)
			&& windows.Count > 0)
		{
			window = windows[0];
			return true;
		}

		window = null!;
		return false;
	}

	private static void CloseWindow(CardPlay cardPlay)
	{
		ulong playerId = cardPlay.Player.NetId;
		if (!WindowsByPlayer.TryGetValue(playerId, out List<ResolutionWindow>? windows))
		{
			return;
		}

		for (int i = windows.Count - 1; i >= 0; i--)
		{
			if (ReferenceEquals(windows[i].RootPlay, cardPlay))
			{
				windows.RemoveAt(i);
				break;
			}
		}

		if (windows.Count == 0)
		{
			WindowsByPlayer.Remove(playerId);
		}
	}

	private static void CloseAbandonedWindow(CardModel card)
	{
		ulong playerId = card.Owner.NetId;
		if (!WindowsByPlayer.TryGetValue(playerId, out List<ResolutionWindow>? windows))
		{
			return;
		}

		for (int i = windows.Count - 1; i >= 0; i--)
		{
			if (ReferenceEquals(windows[i].RootCard, card))
			{
				windows.RemoveAt(i);
				break;
			}
		}

		if (windows.Count == 0)
		{
			WindowsByPlayer.Remove(playerId);
		}
	}

	private static async Task CloseWindowAfterPlay(Task playTask, CardModel card)
	{
		try
		{
			await playTask;
		}
		finally
		{
			CloseAbandonedWindow(card);
		}
	}

	/// <summary>
	/// AfterCardPlayed closes normal windows. This wrapper is the exception-safe fallback for a
	/// failed manual play, because vanilla does not invoke AfterCardPlayed when OnPlay throws.
	/// </summary>
	[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
	private static class OnPlayWrapperPatch
	{
		private static void Postfix(
			CardModel __instance,
			bool isAutoPlay,
			ref Task __result)
		{
			if (!isAutoPlay)
			{
				__result = CloseWindowAfterPlay(__result, __instance);
			}
		}
	}
}
