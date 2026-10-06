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
		if (cardPlay.IsAutoPlay
			|| !CardResolutionTracker.IsOutermostCardPlay(cardPlay)
			|| !cardPlay.Card.HasModKeyword(SquKeywords.SupremeGeneral))
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
		if (!props.IsPoweredAttack()
			|| cardSource is null
			|| !TryGetInheritedVigorBonus(cardSource, cardPlay, out decimal inheritedVigor)
			|| dealer != cardSource.Owner.Creature)
		{
			return 0m;
		}

		return inheritedVigor;
	}

	internal static bool TryGetInheritedVigorBonus(
		CardModel card,
		CardPlay? cardPlay,
		out decimal inheritedVigor)
	{
		inheritedVigor = 0m;
		if (!TryGetSupremeGeneralChildWindow(card, cardPlay, out ResolutionWindow window)
			|| window.ConsumedVigor <= 0m)
		{
			return false;
		}

		inheritedVigor = SquVigorSnapshot.ApplyBonusPercentage(
			window.RootCard.Owner.Creature,
			window.RootCard,
			window.ConsumedVigor);
		return true;
	}

	internal static bool ShouldSuppressSameOwnerChildAttackResources(
		CardModel card,
		CardPlay? cardPlay) =>
		TryGetSupremeGeneralChildWindow(card, cardPlay, out ResolutionWindow window)
		&& card.Owner == window.RootCard.Owner;

	/// <summary>
	/// Lets a non-damaging Supreme General root card consume Vigor as though it had issued its own
	/// powered Attack, then exposes the amount actually consumed to its nested auto-played Attacks.
	/// </summary>
	internal static async Task ConsumeVigorForRootWithoutAttack(
		PlayerChoiceContext choiceContext,
		CardPlay cardPlay)
	{
		if (!TryGetOutermostWindow(cardPlay.Player.NetId, out ResolutionWindow window)
			|| !ReferenceEquals(window.RootPlay, cardPlay))
		{
			return;
		}

		Creature owner = cardPlay.Player.Creature;
		decimal vigorBefore = GetVigorAmount(owner);
		await SquVigorSnapshot.SpendAll(choiceContext, owner, cardPlay.Card);
		decimal consumed = vigorBefore - GetVigorAmount(owner);
		if (consumed > 0m)
		{
			window.ConsumedVigor += consumed;
		}
	}

	private static bool TryGetSupremeGeneralChildWindow(
		CardModel card,
		CardPlay? cardPlay,
		out ResolutionWindow window)
	{
		window = null!;
		bool isAutoPlayedResolution = cardPlay is not null
			? cardPlay.IsAutoPlay && ReferenceEquals(cardPlay.Card, card)
			: card.Pile?.Type == PileType.Play
				&& CardResolutionTracker.IsBeingAutoPlayed(card);

		if (!isAutoPlayedResolution || card.Type != CardType.Attack)
		{
			return false;
		}

		if (TryGetOutermostWindow(card.Owner.NetId, out ResolutionWindow sameOwnerWindow)
			&& IsActiveSupremeGeneralWindow(sameOwnerWindow, card))
		{
			window = sameOwnerWindow;
			return true;
		}

		ResolutionWindow? crossOwnerWindow = null;
		foreach ((ulong playerId, List<ResolutionWindow> windows) in WindowsByPlayer)
		{
			if (playerId == card.Owner.NetId || windows.Count == 0)
			{
				continue;
			}

			ResolutionWindow candidate = windows[0];
			if (!IsActiveSupremeGeneralWindow(candidate, card))
			{
				continue;
			}

			// More than one active cross-player window would be ambiguous. Do not grant a
			// bonus instead of choosing a different root on different peers.
			if (crossOwnerWindow is not null)
			{
				return false;
			}

			crossOwnerWindow = candidate;
		}

		if (crossOwnerWindow is null)
		{
			return false;
		}

		window = crossOwnerWindow;
		return true;
	}

	private static bool IsActiveSupremeGeneralWindow(
		ResolutionWindow window,
		CardModel childCard) =>
		!ReferenceEquals(childCard, window.RootCard)
		&& CardResolutionTracker.TryGetOutermostCard(
			window.RootCard.Owner,
			out CardModel outermostCard)
		&& ReferenceEquals(outermostCard, window.RootCard);

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
