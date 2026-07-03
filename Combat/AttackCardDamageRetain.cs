using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Permanently increases an attack card's base damage for the rest of combat, mirroring
/// <see cref="MegaCrit.Sts2.Core.Models.Cards.Thrash"/>.
/// </summary>
public static class AttackCardDamageRetain
{
	public static bool TryAddBaseDamage(CardModel card, decimal amount)
	{
		if (amount <= 0)
		{
			return false;
		}

		if (card.DynamicVars.ContainsKey("Damage"))
		{
			card.DynamicVars.Damage.BaseValue += amount;
			return true;
		}

		if (card.DynamicVars.ContainsKey("CalculatedDamage"))
		{
			card.DynamicVars.CalculatedDamage.BaseValue += amount;
			return true;
		}

		if (card.DynamicVars.ContainsKey("OstyDamage"))
		{
			card.DynamicVars.OstyDamage.BaseValue += amount;
			return true;
		}

		Squ.SquMod.Logger?.Warn(
			$"Could not retain {amount} damage on {card.Id.Entry}: no recognized damage dynamic var.");
		return false;
	}
}
