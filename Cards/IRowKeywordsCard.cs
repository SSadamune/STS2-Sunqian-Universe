namespace Squ.Cards;

/// <summary>
/// Marks a card whose adjacent automatic keyword declarations should share a row.
/// Keywords before and after the rules text remain separate groups.
/// </summary>
public interface IRowKeywordsCard
{
	/// <summary>Also joins trailing keywords to the final rules-text line when enabled.</summary>
	bool MergeTrailingKeywordsWithLastRule => false;
}
