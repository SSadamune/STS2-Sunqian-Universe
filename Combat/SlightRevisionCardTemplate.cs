using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Combat;

/// <summary>Base card for a built-in Slight Revision target.</summary>
public abstract class SlightRevisionCardTemplate<TTarget> : ModCardTemplate, ISlightRevisionSource
	where TTarget : CardModel
{
	protected SlightRevisionCardTemplate(int cost, CardType type, CardRarity rarity, TargetType targetType)
		: base(cost, type, rarity, targetType) { }

	public override IEnumerable<CardKeyword> CanonicalKeywords => [SquKeywords.SlightRevision];

	/// <summary>Whether Slight Revision should create an upgraded target card.</summary>
	protected virtual bool IsSlightRevisionTargetUpgraded => IsUpgraded;

	CardModel ISlightRevisionSource.SlightRevisionTarget => ModelDb.Card<TTarget>();

	bool ISlightRevisionSource.SlightRevisionTargetUpgraded => IsSlightRevisionTargetUpgraded;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		SlightRevisionSystem.GetHoverTips(ModelDb.Card<TTarget>(), IsSlightRevisionTargetUpgraded);

	protected override void AddExtraArgsToDescription(LocString description) =>
		SlightRevisionSystem.AddDescription(description, ModelDb.Card<TTarget>(), IsSlightRevisionTargetUpgraded);

}
