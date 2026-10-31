using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Squ.Cards;

/// <summary>
/// Marks a card whose adjacent automatic keyword declarations should share a row.
/// Keywords before and after the rules text remain separate groups.
/// <see cref="KeywordsBelowRules"/> moves selected keywords the card actually has
/// to just below the rules text, above keywords that already belong there.
/// When the card has a charge paragraph, those keywords join the front of that line.
/// </summary>
public interface IRowKeywordsCard
{
	bool MergeKeywordRows => true;
	/// <summary>Also joins trailing keywords to the final rules-text line when enabled.</summary>
	bool MergeTrailingKeywordsWithLastRule => false;
	/// <summary>Keywords rendered below the rules text, above the card's other trailing keywords.</summary>
	IEnumerable<CardKeyword> KeywordsBelowRules => [];
}
