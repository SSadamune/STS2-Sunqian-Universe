using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Squ;
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
		bool isMultiTarget)
		: base(cost, type, rarity, targetType, isMultiTarget)
	{
	}

	protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PlayScriptAsync(choiceContext, cardPlay);
	}

	protected abstract Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay);

	protected override HashSet<CardTag> CanonicalTags => [SquCardTags.Script];

	public override IEnumerable<CardKeyword> CanonicalKeywords => [SquKeywords.Script];
}
