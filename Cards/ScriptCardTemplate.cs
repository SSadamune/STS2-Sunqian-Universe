using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Squ;
using Squ.Script;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 带「剧本」标签的卡牌基类：打出时执行具体效果。
/// </summary>
public abstract class ScriptCardTemplate : ModCardTemplate, IRowKeywordsCard
{
	protected ScriptCardTemplate(
		int cost,
		CardType type,
		CardRarity rarity,
		TargetType targetType,
		bool _)
		: base(cost, type, rarity, targetType, showInCardLibrary: true)
	{
	}

	protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PlayScriptAsync(choiceContext, cardPlay);
	}

	protected abstract Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay);

	protected virtual IEnumerable<IHoverTip> AdditionalScriptHoverTips => [];

	protected sealed override IEnumerable<IHoverTip> AdditionalHoverTips
	{
		get
		{
			IEnumerable<IHoverTip> tips = AdditionalScriptHoverTips;
			foreach (var record in CreateScriptRecordPreviews())
				tips = tips.Append(new CardHoverTip(record)).Concat(record.NestedHoverTips);
			return IHoverTip.RemoveDupes(tips);
		}
	}


	internal abstract ScriptRecordCardTemplate CreateScriptRecordPreview();

	internal virtual IEnumerable<ScriptRecordCardTemplate> CreateScriptRecordPreviews() =>
		[CreateScriptRecordPreview()];

	internal virtual int GetScriptPowerAmountForPreview() => 1;

	internal abstract bool IsReinforceableScript { get; }

	internal abstract bool HasUpgradedScriptEffect { get; }

	protected virtual bool UsesReinforceActivationText => IsReinforceableScript;

	protected virtual bool MergeScriptActivationKeywords => !IsReinforceableScript;

	protected virtual bool UsesRowKeywordTemplate => true;
	bool IRowKeywordsCard.MergeKeywordRows => UsesRowKeywordTemplate;

	bool IRowKeywordsCard.MergeTrailingKeywordsWithLastRule => MergeScriptActivationKeywords;

	protected override HashSet<CardTag> CanonicalTags => [SquCardTags.Script];

	public override IEnumerable<CardKeyword> CanonicalKeywords => [SquKeywords.Script];
}

public abstract class ScriptCardTemplate<TRecord> : ScriptCardTemplate
	where TRecord : ScriptRecordCardTemplate
{
	protected ScriptCardTemplate(
		int cost,
		CardType type,
		CardRarity rarity,
		TargetType targetType,
		bool value)
		: base(cost, type, rarity, targetType, value)
	{
	}

	internal sealed override bool IsReinforceableScript =>
		ModelDb.Card<TRecord>().CanBeReinforced;

	protected override bool UsesReinforceActivationText =>
		IsReinforceableScript && !ModelDb.Card<TRecord>().UsesMaterialReuse;

	internal sealed override bool HasUpgradedScriptEffect =>
		ModelDb.Card<TRecord>().HasUpgradedVersion;

	internal sealed override ScriptRecordCardTemplate CreateScriptRecordPreview()
	{
		var preview = (TRecord)ModelDb.Card<TRecord>().ToMutable();
		preview.PreviewFrom(this);
		return preview;
	}

	protected Task UnlockScriptAsync(
		PlayerChoiceContext choiceContext,
		decimal amount = 1m,
		System.Action<TRecord>? configureRecord = null,
		System.Action<TRecord>? afterActivation = null) =>
		ScriptSystem.UnlockOrReinforceAsync(
			choiceContext,
			this,
			amount,
			configureRecord,
			afterActivation);

	protected override void AddExtraArgsToDescription(LocString description)
	{
		base.AddExtraArgsToDescription(description);
		string key = UsesReinforceActivationText
			? "SUNQIAN_UNIVERSE_CARD_SCRIPT_ACTIVATE_REINFORCE"
			: "SUNQIAN_UNIVERSE_CARD_SCRIPT_ACTIVATE";
		if (HasUpgradedScriptEffect)
		{
			key += "_UPGRADABLE";
		}
		LocString text = new("cards", key);
		SquKeywords.AddNestedLoc(description, "ScriptUnlockText", text);
	}
}
