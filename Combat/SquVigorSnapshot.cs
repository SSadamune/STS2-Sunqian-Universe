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
	public static decimal? DamagePerHitWithSnapshot(CardModel card, int vigorSnapshot, bool isFollowUpAttack)
	{
		if (!isFollowUpAttack || vigorSnapshot <= 0)
		{
			return null;
		}

		return card.DynamicVars.Damage.BaseValue + vigorSnapshot;
	}

	/// <summary>
	/// Tracks vigor consumption across multiple <see cref="MegaCrit.Sts2.Core.Commands.AttackCommand"/>s
	/// from one card play. Call <see cref="ResolveNextAttackDamage"/> once per attack command.
	/// </summary>
	public sealed class AttackSequence
	{
		private readonly CardModel _card;
		private readonly int _vigorSnapshot;
		private bool _firstAttackDone;

		public AttackSequence(Creature dealer, CardModel card)
		{
			_card = card;
			_vigorSnapshot = GetAmount(dealer);
		}

		public int VigorSnapshot => _vigorSnapshot;

		public decimal ResolveNextAttackDamage()
		{
			decimal baseDamage = _card.DynamicVars.Damage.BaseValue;
			decimal? snapshotted = DamagePerHitWithSnapshot(_card, _vigorSnapshot, _firstAttackDone);
			_firstAttackDone = true;
			return snapshotted ?? baseDamage;
		}
	}

	public static AttackSequence BeginAttackSequence(Creature dealer, CardModel card) =>
		new(dealer, card);
}
