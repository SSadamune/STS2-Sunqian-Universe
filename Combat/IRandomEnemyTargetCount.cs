using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Cards using <see cref="SquTargetTypes.RandomEnemies"/> declare how many distinct random enemies to hit.
/// </summary>
public interface IRandomEnemyTargetCount
{
	int GetRandomEnemyTargetCount();
}
