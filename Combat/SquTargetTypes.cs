using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using Squ.Powers;
using STS2RitsuLib.Combat.CardTargeting;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Mod-scoped <see cref="TargetType"/> values for random multi-enemy targeting
/// and condition-gated single targets such as burning enemies.
/// </summary>
public static class SquTargetTypes
{
	public static TargetType RandomEnemies { get; private set; }

	public static TargetType AnyBurningEnemy { get; private set; }

	public static TargetType AnyPlayer { get; private set; }

	public static TargetType AnyOtherPlayer { get; private set; }

	public static void Register()
	{
		RandomEnemies = CustomTargetType.RegisterMultiTargetType(
			SquMod.ModId,
			"random_enemies",
			static (Creature creature, Player player) =>
				creature.IsAlive && creature.Side != player.Creature.Side);

		AnyBurningEnemy = CustomTargetType.RegisterSingleTargetType(
			SquMod.ModId,
			"any_burning_enemy",
			static (Creature creature, Player player) =>
				creature.IsAlive
				&& creature.Side != player.Creature.Side
				&& creature.HasPower<BurningPower>());

		AnyPlayer = CustomTargetType.RegisterSingleTargetType(
			SquMod.ModId,
			"any_player",
			static (Creature creature, Player player) =>
				creature.IsAlive && creature.IsPlayer);

		AnyOtherPlayer = CustomTargetType.RegisterSingleTargetType(
			SquMod.ModId,
			"any_other_player",
			static (Creature creature, Player player) =>
				creature.IsAlive && creature.IsPlayer && creature != player.Creature);
	}

	public static bool IsRandomEnemiesTarget(TargetType type) => type == RandomEnemies;
}
