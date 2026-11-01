using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Interop;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Records Vigor and Drunken Might debits attributable to an explicitly registered set of
/// Attack card resolutions. Positive write-backs never reduce the recorded debit, and each
/// resource is capped at the amount present when the transaction began. This keeps nested
/// auto-plays from leaking into their parent unless the caller deliberately registers them.
/// </summary>
public sealed class AttackResourceSpendTransaction
{
	private readonly Creature _owner;
	private readonly HashSet<CardModel> _sources = [];
	private readonly decimal _vigorBudget;
	private readonly decimal _drunkenMightBudget;

	public AttackResourceSpendTransaction(Creature owner, CardModel rootCard)
	{
		_owner = owner;
		_vigorBudget = decimal.Max(0m, owner.GetPower<VigorPower>()?.Amount ?? 0m);
		_drunkenMightBudget = NewsanguoDrunkenMightEffect.GetAmount(owner);
		_sources.Add(rootCard);
	}

	public decimal VigorSpent { get; private set; }

	public decimal DrunkenMightSpent { get; private set; }

	public bool SpentAny => VigorSpent > 0m || DrunkenMightSpent > 0m;

	/// <summary>
	/// Amount that can safely be restored without raising Vigor above its opening value.
	/// </summary>
	public decimal GetRestorableVigor()
	{
		decimal current = _owner.GetPower<VigorPower>()?.Amount ?? 0m;
		return decimal.Min(VigorSpent, decimal.Max(0m, _vigorBudget - current));
	}

	/// <summary>
	/// Amount that can safely be restored without raising Drunken Might above its opening value.
	/// This deliberately ignores temporary upstream write-backs during nested card resolution.
	/// </summary>
	public decimal GetRestorableDrunkenMight()
	{
		decimal current = NewsanguoDrunkenMightEffect.GetAmount(_owner);
		return decimal.Min(
			DrunkenMightSpent,
			decimal.Max(0m, _drunkenMightBudget - current));
	}

	/// <summary>
	/// Adds a nested Attack to this logical transaction. Callers that do not want child attacks
	/// included simply never register them.
	/// </summary>
	public void RegisterSource(CardModel card)
	{
		if (card.Owner.Creature == _owner && card.Type == CardType.Attack)
		{
			_sources.Add(card);
		}
	}

	/// <summary>
	/// Records only exact, negative resource changes. Vanilla Vigor can recover its omitted source
	/// from the Attack command it binds internally. Other source-less events are intentionally
	/// ignored: under-triggering is safer than creating or refunding resources after an ambiguous
	/// nested auto-play.
	/// </summary>
	public void Record(PowerModel power, decimal amount, CardModel? cardSource)
	{
		if (cardSource is null
			&& power is VigorPower vigor
			&& AttackVigorResolution.TryGetBoundAttackCard(vigor, out CardModel vigorCard))
		{
			cardSource = vigorCard;
		}

		if (amount >= 0m
			|| cardSource is null
			|| !_sources.Contains(cardSource)
			|| !NewsanguoDrunkenMightEffect.IsEffectResource(power, _owner))
		{
			return;
		}

		decimal debit = -amount;
		if (power is VigorPower)
		{
			VigorSpent += decimal.Min(debit, _vigorBudget - VigorSpent);
		}
		else
		{
			DrunkenMightSpent += decimal.Min(debit, _drunkenMightBudget - DrunkenMightSpent);
		}
	}
}
