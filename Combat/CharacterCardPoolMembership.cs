#nullable enable

using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Squ.Combat;

/// <summary>
/// Resolves character-card membership the same way as Splash: enumerate the
/// player's character pools and inspect each pool's registered card ids.
/// </summary>
internal static class CharacterCardPoolMembership
{
	public static IReadOnlyList<CardPoolModel> GetCharacterPools(Player player) =>
		player.UnlockState.CharacterCardPools
			.GroupBy(pool => pool.Id)
			.Select(group => group.First())
			.ToList();

	public static IReadOnlyList<CardPoolModel> GetOtherCharacterPools(Player player)
	{
		List<CardPoolModel> pools = GetCharacterPools(player).ToList();
		if (pools.Count > 1)
		{
			pools.RemoveAll(pool => pool.Id == player.Character.CardPool.Id);
		}

		return pools;
	}

	public static IEnumerable<CardModel> GetUnlockedCards(
		Player player,
		IEnumerable<CardPoolModel> pools) =>
		pools.SelectMany(pool => pool.GetUnlockedCards(
			player.UnlockState,
			player.RunState.CardMultiplayerConstraint));

	public static bool Contains(CardPoolModel pool, CardModel card) =>
		pool.AllCardIds.Contains(card.Id);

	public static bool IsColorless(CardModel card) =>
		ModelDb.AllSharedCardPools.Any(pool =>
			pool.IsColorless && Contains(pool, card));
}
