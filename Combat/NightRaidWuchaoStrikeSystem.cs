#nullable enable
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using Squ.Powers;
using STS2RitsuLib;
using STS2RitsuLib.Models.Capabilities;

namespace Squ.Combat;

/// <summary>将夜袭乌巢的通用展示能力附加到任意来源的打击牌。</summary>
public static class NightRaidWuchaoStrikeSystem
{
	private static bool _initialized;

	public static void Initialize()
	{
		if (_initialized)
		{
			return;
		}

		_initialized = true;
		RitsuLibFramework.SubscribeLifecycle<CardMovedBetweenPilesEvent>(evt =>
		{
			if (GetBaseBurning(evt.Card) > 0m)
			{
				EnsureCapability(evt.Card);
			}
		});
	}

	public static void EnsureCapabilities(Player player)
	{
		IEnumerable<CardModel> cards = player.Deck.Cards;
		if (player.PlayerCombatState is { } combatState)
		{
			cards = cards.Concat(combatState.AllCards);
		}

		foreach (CardModel card in cards.Distinct())
		{
			EnsureCapability(card);
		}
	}

	public static void RefreshOnTable(Player player)
	{
		if (player.PlayerCombatState is not { } combatState)
		{
			return;
		}

		foreach (CardModel card in combatState.AllCards.Where(IsOnTableStrike))
		{
			NCard.FindOnTable(card)?.UpdateVisuals(card.Pile!.Type, CardPreviewMode.Normal);
		}
	}

	public static decimal GetBaseBurning(CardModel? card)
	{
		if (card is not { IsMutable: true } || !card.Tags.Contains(CardTag.Strike))
		{
			return 0m;
		}

		return card.Owner?.Creature?.GetPower<ScriptNightRaidWuchaoPower>() is { Amount: > 0 } power
			? power.Amount
			: 0m;
	}

	private static void EnsureCapability(CardModel card)
	{
		if (card.IsMutable
			&& card.Tags.Contains(CardTag.Strike)
			&& card.Capability<NightRaidWuchaoStrikeCapability>() is null)
		{
			card.GetOrCreateCapability<NightRaidWuchaoStrikeCapability>();
		}

		if (IsOnTableStrike(card))
		{
			NCard.FindOnTable(card)?.UpdateVisuals(card.Pile!.Type, CardPreviewMode.Normal);
		}
	}

	private static bool IsOnTableStrike(CardModel card) =>
		card.Tags.Contains(CardTag.Strike)
		&& card.Pile?.Type is PileType.Hand or PileType.Play;
}
