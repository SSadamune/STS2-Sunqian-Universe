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

/// <summary>Attaches Chicken Foot Cheese presentation data to basic Strikes from every character and mod.</summary>
public static class ChickenFootCheeseStrikeSystem
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
			if (ScriptChickenFootCheesePower.ShouldRedirectBasicStrike(evt.Card))
			{
				EnsureCapability(evt.Card, refreshIfAdded: true);
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

		foreach (CardModel card in cards.Distinct().Where(
			ScriptChickenFootCheesePower.ShouldRedirectBasicStrike))
		{
			EnsureCapability(card, refreshIfAdded: false);
		}
	}

	private static void EnsureCapability(CardModel card, bool refreshIfAdded)
	{
		if (!card.IsMutable || card.Capability<ChickenFootCheeseStrikeCapability>() is not null)
		{
			return;
		}

		card.GetOrCreateCapability<ChickenFootCheeseStrikeCapability>();
		if (refreshIfAdded && card.Pile?.Type is PileType.Hand or PileType.Play)
		{
			NCard.FindOnTable(card)?.UpdateVisuals(card.Pile.Type, CardPreviewMode.Normal);
		}
	}
}
