using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Helpers for preserving <see cref="VigorPower"/> across multiple <see cref="MegaCrit.Sts2.Core.Commands.AttackCommand"/>s
/// from a single card play.
/// </summary>
public static class SquVigorSnapshot
{
	public static int GetAmount(Creature creature) =>
		creature.GetPower<VigorPower>() is { Amount: > 0 } vigor ? vigor.Amount : 0;

	/// <summary>
	/// Returns card base damage plus a snapshotted vigor bonus for follow-up attacks after the first
	/// <see cref="AttackCommand"/> has consumed <see cref="VigorPower"/>.
	/// </summary>
	public static decimal? DamagePerHitWithSnapshot(CardModel card, int vigorSnapshot, bool isFollowUpWave)
	{
		if (!isFollowUpWave || vigorSnapshot <= 0)
		{
			return null;
		}

		return card.DynamicVars.Damage.BaseValue + vigorSnapshot;
	}
}
