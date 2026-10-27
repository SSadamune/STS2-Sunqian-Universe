using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Interop;
using Squ.Powers;
using STS2RitsuLib;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Holds a Headsman reward until the outer attack card has completely resolved.
/// The source power may be replaced by a Script activated near the end of that same
/// card (notably Part-Time General), so the pending reward must outlive the power.
/// </summary>
internal static class HeadsmanRewardTracker
{
	private sealed class AttackPlayTrack
	{
		public required Creature Owner { get; init; }

		public required ScriptHeadsmanPower SourcePower { get; init; }

		public required int EnergySpent { get; init; }

		public required AttackResourceSpendTransaction ResourceSpend { get; init; }

		public bool AnyKill { get; set; }
	}

	private static readonly Dictionary<CardModel, AttackPlayTrack> ActivePlays = [];
	private static bool _initialized;

	internal static void Initialize()
	{
		if (_initialized)
		{
			return;
		}

		_initialized = true;
		RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(_ => ActivePlays.Clear());
		RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(_ => ActivePlays.Clear());
	}

	internal static void Begin(
		ScriptHeadsmanPower sourcePower,
		CardPlay cardPlay)
	{
		ActivePlays[cardPlay.Card] = new AttackPlayTrack
		{
			Owner = sourcePower.Owner,
			SourcePower = sourcePower,
			EnergySpent = cardPlay.Resources.EnergySpent,
			ResourceSpend = new AttackResourceSpendTransaction(
				sourcePower.Owner,
				cardPlay.Card),
		};
	}

	internal static void RecordKill(Creature owner)
	{
		if (TryGetRootTrack(owner, out AttackPlayTrack? track))
		{
			track.AnyKill = true;
		}
	}

	internal static void RecordResourceSpend(
		Creature owner,
		PowerModel power,
		decimal amount,
		CardModel? cardSource)
	{
		if (TryGetRootTrack(owner, out AttackPlayTrack? track))
		{
			track.ResourceSpend.Record(power, amount, cardSource);
		}
	}

	internal static async Task ResolveAsync(
		PlayerChoiceContext choiceContext,
		CardModel rootCard)
	{
		if (!ActivePlays.Remove(rootCard, out AttackPlayTrack? track)
			|| !track.AnyKill
			|| track.Owner.IsDead)
		{
			return;
		}

		bool restored = false;
		decimal vigorToRestore = track.ResourceSpend.GetRestorableVigor();
		if (vigorToRestore > 0m)
		{
			await PowerCmd.Apply<VigorPower>(
				choiceContext,
				track.Owner,
				vigorToRestore,
				track.Owner,
				rootCard);
			if (track.Owner.GetPower<VigorPower>() is { } restoredVigor)
			{
				AttackVigorResolution.ClearVigorAttackBinding(restoredVigor);
			}
			restored = true;
		}

		decimal drunkenMightToRestore = track.ResourceSpend.GetRestorableDrunkenMight();
		if (drunkenMightToRestore > 0m)
		{
			await NewsanguoPublicApiInterop.ApplyDrunkenMight(
				choiceContext,
				track.Owner,
				drunkenMightToRestore,
				track.Owner,
				rootCard);
			restored = true;
		}

		if (track.EnergySpent > 0 && track.Owner.Player is Player player)
		{
			await PlayerCmd.GainEnergy(track.EnergySpent, player);
			restored = true;
		}

		if (restored && track.Owner.Powers.Contains(track.SourcePower))
		{
			track.SourcePower.FlashReward();
		}
	}

	internal static Task ResolveAfterCardAsync(CardModel rootCard) =>
		ResolveAsync(new BlockingPlayerChoiceContext(), rootCard);

	internal static void Discard(CardModel rootCard) => ActivePlays.Remove(rootCard);

	private static bool TryGetRootTrack(
		Creature owner,
		out AttackPlayTrack track)
	{
		track = null!;
		return CardResolutionTracker.TryGetOutermostCard(owner.Player, out CardModel rootCard)
			&& rootCard.Type == CardType.Attack
			&& rootCard.Owner.Creature == owner
			&& ActivePlays.TryGetValue(rootCard, out track!);
	}
}
