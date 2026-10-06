using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Powers;

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
	/// Returns the Vigor bonus actually granted to this card, including additive percentage
	/// bonuses from 吾亦过江 and 活力增幅. This also covers non-damage effects that explicitly
	/// scale with Vigor.
	/// </summary>
	public static int GetEffectiveAmount(Creature creature, CardModel card)
	{
		int vigor = GetAmount(creature);
		if (card.Type != CardType.Attack || vigor <= 0)
		{
			return vigor;
		}

		return (int)ApplyBonusPercentage(creature, card, vigor);
	}

	/// <summary>
	/// Applies additive Vigor bonus percentages to a snapshotted amount, including Vigor inherited
	/// by Supreme General child Attacks after the original Vigor power has been consumed.
	/// </summary>
	public static decimal ApplyBonusPercentage(
		Creature creature,
		CardModel card,
		decimal vigor)
	{
		if (card.Type != CardType.Attack || vigor <= 0m)
		{
			return vigor;
		}

		int bonusPercent = 0;
		if (creature.GetPower<CrossTheRiverPower>() is { Amount: > 0 })
		{
			bonusPercent += 100;
		}

		if (creature.GetPower<VigorAmplificationPower>() is { } amplification)
		{
			bonusPercent += amplification.GetBonusMultiplierFor(card) * 100;
		}

		return vigor * (100 + bonusPercent) / 100m;
	}

	/// <summary>
	/// 卡面绿字只在手牌或打出过程中计入活力，与原版攻击伤害预览一致。
	/// </summary>
	public static int GetAmountForCardPreview(CardModel card) =>
		card.Pile?.Type is PileType.Hand or PileType.Play && card.Owner?.Creature is { } owner
			? GetEffectiveAmount(owner, card)
			: 0;

	/// <summary>
	/// Spends all current <see cref="VigorPower"/> through <see cref="PowerCmd.ModifyAmount"/>,
	/// so listeners such as 平湖惊雷 still see a negative amount change.
	/// </summary>
	public static async Task<int> SpendAll(
		PlayerChoiceContext choiceContext,
		Creature creature,
		CardModel? cardSource)
	{
		VigorPower? vigor = creature.GetPower<VigorPower>();
		if (vigor is not { Amount: > 0 })
		{
			return 0;
		}

		int spent = vigor.Amount;
		await PowerCmd.ModifyAmount(choiceContext, vigor, -spent, creature, cardSource);
		return spent;
	}

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
			_vigorSnapshot = GetEffectiveAmount(dealer, card);
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
