using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Combat.CardTargeting;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Mod-scoped <see cref="TargetType"/> values for random multi-enemy targeting.
/// </summary>
public static class SquTargetTypes
{
	public static TargetType RandomEnemies { get; private set; }

	public static void Register()
	{
		RandomEnemies = CustomTargetType.RegisterMultiTargetType(
			SquMod.ModId,
			"random_enemies",
			static (Creature creature, Player player) =>
				creature.IsAlive && creature.Side != player.Creature.Side);
	}

	public static bool IsRandomEnemiesTarget(TargetType type) => type == RandomEnemies;
}
