using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Combat;

/// <summary>Base card for a built-in Slight Revision target.</summary>
public abstract class SlightRevisionCardTemplate<TTarget> : ModCardTemplate, IModRightClickableCard
	where TTarget : CardModel
{
	protected SlightRevisionCardTemplate(int cost, CardType type, CardRarity rarity, TargetType targetType)
		: base(cost, type, rarity, targetType) { }

	public override IEnumerable<CardKeyword> CanonicalKeywords => [SquKeywords.SlightRevision];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		SlightRevisionSystem.GetHoverTips(ModelDb.Card<TTarget>(), IsUpgraded);

	protected override void AddExtraArgsToDescription(LocString description) =>
		SlightRevisionSystem.AddDescription(description, ModelDb.Card<TTarget>(), IsUpgraded);

	public bool CanHandleRightClickLocal(ModRightClickContext context) =>
		context.Player == Owner && Pile?.Type == PileType.Hand && IsTransformable;

	public Task OnRightClick(ModRightClickExecutionContext context) =>
		SlightRevisionSystem.TransformAsync(this, ModelDb.Card<TTarget>(), IsUpgraded);
}
