using System;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Xunit;

namespace TestTheSpire;

public static class CardHoverTipAssertions
{
	public static void ContainsPower<TPower>(CardModel card) where TPower : PowerModel
	{
		string expectedId = HoverTipFactory.FromPower<TPower>((int?)null).Id;
		Assert.Contains<IHoverTip>(card.HoverTips, (Predicate<IHoverTip>)((IHoverTip tip) => tip.Id == expectedId));
	}

	public static void DoesNotContainPower<TPower>(CardModel card) where TPower : PowerModel
	{
		string expectedId = HoverTipFactory.FromPower<TPower>((int?)null).Id;
		Assert.DoesNotContain<IHoverTip>(card.HoverTips, (Predicate<IHoverTip>)((IHoverTip tip) => tip.Id == expectedId));
	}
}
