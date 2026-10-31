#nullable enable
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Squ.Combat;

/// <summary>
/// Creates generated-card hover tips that preserve the target card's unfinalized upgrade
/// while the source card itself is being shown as an upgrade preview. This lets the game's
/// normal description renderer color every changed value and upgraded text green.
/// </summary>
public static class SquCardHoverTipFactory
{
	public static IHoverTip FromCard<TTarget>(
		CardModel source, bool targetUpgraded, params CardKeyword[] additionalKeywords)
		where TTarget : CardModel
	{
		CardModel preview = CreatePreview(source, ModelDb.Card<TTarget>(), targetUpgraded);
		foreach (CardKeyword keyword in additionalKeywords) preview.AddKeyword(keyword);
		return new CardHoverTip(preview);
	}

	/// <summary>A fixed generated-card preview, independent of the source's upgrade preview.</summary>
	public static IHoverTip FromCard<TTarget>(bool targetUpgraded, params CardKeyword[] additionalKeywords)
		where TTarget : CardModel =>
		FromCard<TTarget>(ModelDb.Card<TTarget>(), targetUpgraded, additionalKeywords);

	public static IHoverTip FromCard(
		CardModel source,
		CardModel target,
		bool targetUpgraded) =>
		new CardHoverTip(CreatePreview(source, target, targetUpgraded));

	public static IEnumerable<IHoverTip> FromCardWithCardHoverTips<TTarget>(
		CardModel source,
		bool targetUpgraded)
		where TTarget : CardModel
	{
		CardModel preview = CreatePreview(source, ModelDb.Card<TTarget>(), targetUpgraded);
		yield return new CardHoverTip(preview);
		foreach (IHoverTip hoverTip in preview.HoverTips)
		{
			yield return hoverTip;
		}
	}

	public static IEnumerable<IHoverTip> FromCardWithCardHoverTips(
		CardModel source,
		CardModel target,
		bool targetUpgraded)
	{
		CardModel preview = CreatePreview(source, target, targetUpgraded);
		yield return new CardHoverTip(preview);
		foreach (IHoverTip hoverTip in preview.HoverTips)
		{
			yield return hoverTip;
		}
	}

	private static CardModel CreatePreview(
		CardModel source,
		CardModel target,
		bool targetUpgraded)
	{
		var preview = (CardModel)target.MutableClone();
		if (!targetUpgraded)
		{
			return preview;
		}

		preview.UpgradePreviewType = source.UpgradePreviewType;
		preview.UpgradeInternal();
		if (source.UpgradePreviewType == CardUpgradePreviewType.None)
		{
			preview.FinalizeUpgradeInternal();
		}

		return preview;
	}
}
