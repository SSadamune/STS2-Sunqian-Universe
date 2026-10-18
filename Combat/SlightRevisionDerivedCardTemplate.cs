using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Squ.Character;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Base class for cards created by Slight Revision. They remain token-pool cards so they are
/// excluded from ordinary rewards and grouped with event/quest/generated cards in the library,
/// while rendering with Sunqian's character-card visuals.
/// </summary>
public abstract class SlightRevisionDerivedCardTemplate : ModCardTemplate
{
	protected SlightRevisionDerivedCardTemplate(
		int cost,
		CardType type,
		TargetType targetType)
		: base(cost, type, CardRarity.Token, targetType, showInCardLibrary: true)
	{
	}

	public override CardPoolModel VisualCardPool => ModelDb.CardPool<SunqianCardPool>();
}
