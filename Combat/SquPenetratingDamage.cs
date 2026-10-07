using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Exposes whether the card's current state upgrades its unblockable damage to piercing damage.
/// </summary>
public interface IPenetratingDamageCard
{
	bool DealsPenetratingDamage { get; }
}

internal static class SquPenetratingDamage
{
	public static bool IsPenetrating(ValueProp props, CardModel? cardSource) =>
		props.HasFlag(ValueProp.Unblockable)
		&& cardSource is IPenetratingDamageCard { DealsPenetratingDamage: true };
}
