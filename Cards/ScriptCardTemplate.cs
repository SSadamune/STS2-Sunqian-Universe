using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Squ;
using Squ.Script;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 带「剧本」标签的卡牌基类：打出时先尝试解除当前剧本能力，再执行具体效果。
/// </summary>
public abstract class ScriptCardTemplate : ModCardTemplate
{
	protected ScriptCardTemplate(
		int cost,
		CardType type,
		CardRarity rarity,
		TargetType targetType,
		bool isMultiTarget)
		: base(cost, type, rarity, targetType, isMultiTarget)
	{
	}

	protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await ScriptSystem.TryLiftScriptAsync(choiceContext, Owner.Creature);
		await PlayScriptAsync(choiceContext, cardPlay);
	}

	protected abstract Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay);

	protected override HashSet<CardTag> CanonicalTags => [SquCardTags.Script];

#pragma warning disable CS0618
	protected override IEnumerable<string> RegisteredKeywordIds => [SquKeywords.ScriptId];
#pragma warning restore CS0618
}
