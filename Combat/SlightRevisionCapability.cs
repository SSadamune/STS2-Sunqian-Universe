using System.Collections.Generic;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models.Capabilities;

#nullable enable

namespace Squ.Combat;

/// <summary>Per-card runtime data and presentation for a granted Slight Revision.</summary>
public sealed class SlightRevisionCapability : CardCapability, ICardDescriptionContributor, ICardHoverTipContributor
{
	public ModelId TargetId { get; private set; } = ModelId.none;

	public bool TargetUpgraded { get; private set; }

	public CardModel Target => ModelDb.GetById<CardModel>(TargetId);

	public SlightRevisionCapability()
	{
	}

	public void Configure(CardModel target, bool targetUpgraded)
	{
		TargetId = target.Id;
		TargetUpgraded = targetUpgraded;
		MarkDirty();
	}

	public IEnumerable<CardDescriptionFragment> GetDescriptionFragments(CardDescriptionContext context)
	{
		LocString text = new("card_keywords", "SUNQIAN_UNIVERSE_KEYWORD_SLIGHT_REVISION.cardDescriptionFragment");
		text.Add("Title", ModKeywordRegistry.GetTitle(SquKeywords.SlightRevisionId));
		text.Add("TargetCardName", SlightRevisionSystem.GetDisplayTitle(Target, TargetUpgraded));
		yield return new CardDescriptionFragment(text, CardDescriptionFragmentPlacement.AfterBase);
	}

	public IEnumerable<IHoverTip> GetHoverTips(CardModel card) =>
		SlightRevisionSystem.GetHoverTips(Target, TargetUpgraded);

	protected override JsonNode? SaveAdditionalState() => new JsonObject
	{
		["targetId"] = TargetId.ToString(),
		["targetUpgraded"] = TargetUpgraded,
	};

	protected override void LoadAdditionalState(JsonNode? state, int schemaVersion)
	{
		if (state is not JsonObject objectState)
		{
			return;
		}

		if (objectState["targetId"]?.GetValue<string>() is { Length: > 0 } serializedTargetId)
		{
			TargetId = ModelId.Deserialize(serializedTargetId);
		}

		TargetUpgraded = objectState["targetUpgraded"]?.GetValue<bool>() ?? false;
	}
}
