using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Shared checks for cards that target all enemies or multiple random enemies.
/// </summary>
public static class MultiTargetCardIntent
{
	public static bool HasMultiTargetIntent(CardModel card)
	{
		if (card.TargetType == TargetType.AllEnemies)
		{
			return true;
		}

		if (SquTargetTypes.IsRandomEnemiesTarget(card.TargetType))
		{
			return SquRandomEnemyTargeting.GetRandomEnemyTargetCount(card) > 1;
		}

		return false;
	}
}
