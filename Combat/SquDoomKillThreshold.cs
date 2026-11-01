using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Powers;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Combines pending Poison, Burning, and Doom damage for the Doom effect threshold.
/// </summary>
public static class SquDoomKillThreshold
{
	public static int GetEffectiveGreenHp(Creature creature)
	{
		// Gameplay deliberately uses stable, coarse stack counts. UI-only preview APIs
		// must never influence CatInTheBox or any other authoritative resolution.
		int poison = creature.GetPowerAmount<PoisonPower>();
		int burning = creature.GetPowerAmount<BurningPower>();
		int doom = creature.GetPowerAmount<DoomPower>();

		return creature.CurrentHp - poison - burning - doom;
	}
}
