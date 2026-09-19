using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Permanently increases a card's printed combat values, mirroring
/// <see cref="MegaCrit.Sts2.Core.Models.Cards.Thrash"/> for damage.
/// </summary>
public static class CardValueRetain
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

	public static bool TryAddBaseValue(CardModel card, string dynamicVarName, decimal amount)
	{
		if (amount <= 0m || !card.DynamicVars.ContainsKey(dynamicVarName))
		{
			return false;
		}

		card.DynamicVars[dynamicVarName].BaseValue += amount;
		return true;
	}
}
