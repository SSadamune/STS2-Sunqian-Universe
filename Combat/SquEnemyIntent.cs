using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Enemy-intent helpers aligned with vanilla cards such as Go for the Eyes.
/// </summary>
public static class SquEnemyIntent
{
	public static bool IntendsToAttack(Creature creature)
	{
		if (creature.Monster?.NextMove?.Intents is not { Count: > 0 } intents)
		{
			return false;
		}

		foreach (AbstractIntent intent in intents)
		{
			if (IsAttackIntent(intent))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsAttackIntent(AbstractIntent intent)
	{
		for (Type? type = intent.GetType(); type != null && type != typeof(object); type = type.BaseType)
		{
			if (NameLooksLikeAttack(type.Name))
			{
				return true;
			}
		}

		return NameLooksLikeAttack(intent.IntentType.ToString());
	}

	private static bool NameLooksLikeAttack(string name) =>
		name.Contains("Attack", StringComparison.OrdinalIgnoreCase)
		|| name.Contains("DeathBlow", StringComparison.OrdinalIgnoreCase)
		|| name.Contains("Explode", StringComparison.OrdinalIgnoreCase);
}
