using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
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
public abstract class ScriptCardTemplate : ModCardTemplate
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

	internal abstract ScriptRecordCardTemplate CreateScriptRecordPreview();

	internal virtual int GetScriptPowerAmountForPreview() => 1;

	internal abstract bool IsReinforceableScript { get; }

	internal abstract string ScriptRecordTitle { get; }

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

	internal sealed override string ScriptRecordTitle => ModelDb.Card<TRecord>().Title;

	internal sealed override ScriptRecordCardTemplate CreateScriptRecordPreview()
	{
		var preview = (TRecord)ModelDb.Card<TRecord>().ToMutable();
		preview.PreviewFrom(this);
		return preview;
	}

	protected Task UnlockScriptAsync(
		PlayerChoiceContext choiceContext,
		decimal amount = 1m,
		System.Action<TRecord>? configureRecord = null) =>
		ScriptSystem.UnlockOrReinforceAsync(
			choiceContext,
			this,
			amount,
			configureRecord);

	protected override void AddExtraArgsToDescription(LocString description)
	{
		base.AddExtraArgsToDescription(description);
		// Card-library entries are canonical models. Accessing Owner on a canonical
		// model throws, so only inspect combat ownership for mutable card instances.
		var owner = IsMutable ? Owner : null;
		bool inCombat = IsMutable
			&& CombatManager.Instance.IsInProgress
			&& owner is not null;
		bool unlocked = inCombat && ScriptSystem.IsUnlocked<TRecord>(owner!);
		string key;
		if (!IsReinforceableScript)
		{
			key = unlocked ? "SCRIPT_UNLOCK_ALREADY" : "SCRIPT_UNLOCK_NEW";
		}
		else if (!inCombat)
		{
			key = "SCRIPT_UNLOCK_OR_REINFORCE";
		}
		else if (unlocked)
		{
			key = "SCRIPT_REINFORCE_AVAILABLE";
		}
		else
		{
			key = "SCRIPT_UNLOCK_AVAILABLE";
		}

		LocString text = new("cards", $"SUNQIAN_UNIVERSE_CARD_{key}");
		text.Add("ScriptTitle", ScriptRecordTitle);
		description.Add("ScriptUnlockText", text.GetFormattedText());
	}
}
