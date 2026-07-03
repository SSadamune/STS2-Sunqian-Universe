using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Powers;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Matches the remaining green segment on the combat health bar:
/// current HP minus forecasted poison, burning, and doom.
/// </summary>
public static class SquDoomKillThreshold
{
	public static int GetEffectiveGreenHp(Creature creature)
	{
		int poison = creature.GetPower<PoisonPower>()?.CalculateTotalDamageNextTurn() ?? 0;
		int burning = creature.GetPower<BurningPower>()?.Amount ?? 0;
		int doom = creature.GetPowerAmount<DoomPower>();

		return creature.CurrentHp - poison - burning - doom;
	}
}
