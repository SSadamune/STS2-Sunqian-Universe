using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Interop;
using Squ.Patches;
using Squ.Powers;
using STS2RitsuLib.Combat.HealthBars;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Places enemy Poison and Burning forecast segments in their real turn-start
/// trigger order. Poison normally uses the game's dedicated overlay, so it is
/// promoted to a regular RitsuLib segment only while both debuffs are present.
/// </summary>
internal sealed class OrderedPoisonHealthBarForecastSource : IHealthBarForecastSource
{
	private static readonly Color PoisonLethalColor = new("76FF40");

	public IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(
		HealthBarForecastContext context)
	{
		Creature target = context.Creature;
		PoisonPower? poison = target.GetPower<PoisonPower>();
		if (poison is null || !UsesSharedEnemyTurnStartSequence(target))
		{
			return [];
		}

		int hpLoss;
		if (!DefenseAwareDebuffPreviewInterop.TryGetPreviewHpLoss(
			target,
			poison,
			out hpLoss))
		{
			hpLoss = PoisonHealthBarForecastPatch.CalculateVanillaPreview(poison);
		}

		return HealthBarForecasts.Single(
			hpLoss,
			PoisonLethalColor,
			HealthBarForecastGrowthDirection.FromRight,
			GetTriggerOrder(target, poison));
	}

	internal static void Register() =>
		HealthBarForecastRegistry.Register(
			SquMod.ModId,
			"ordered-poison-turn-start",
			new OrderedPoisonHealthBarForecastSource());

	internal static bool UsesSharedEnemyTurnStartSequence(Creature target) =>
		target.IsEnemy
		&& !target.IsPet
		&& target.HasPower<PoisonPower>()
		&& target.HasPower<BurningPower>();

	internal static int GetTriggerOrder(Creature target, PowerModel source)
	{
		if (!UsesSharedEnemyTurnStartSequence(target))
		{
			return 0;
		}

		foreach (PowerModel power in target.Powers)
		{
			if (power is not PoisonPower and not BurningPower)
			{
				continue;
			}

			return ReferenceEquals(power, source) ? 0 : 1;
		}

		return 0;
	}
}
