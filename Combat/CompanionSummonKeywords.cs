#nullable enable
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Squ.Combat;

/// <summary>Parameterized text clauses, like vanilla SummonVar rather than CardKeyword.</summary>
public static class CompanionSummonKeywords
{
	public static IHoverTip NamedHoverTip(string id, DynamicVar amount) =>
		new HoverTip(Text("namedTitle", id, amount), Text("namedDescription", id, amount));
	public static IHoverTip AnyHoverTip(DynamicVar amount) =>
		new HoverTip(Text("anyTitle", null, amount), Text("anyDescription", null, amount));
	public static LocString NamedCardText(string id, DynamicVar amount) =>
		Text("namedCardText", id, amount);
	public static LocString AnyCardText(DynamicVar amount) => Text("anyCardText", null, amount);

	private static LocString Text(string suffix, string? id, DynamicVar amount)
	{
		LocString result = new("card_keywords", "SUNQIAN_UNIVERSE_SUMMON." + suffix);
		result.AddObj("Summon", amount);
		result.Add("SummonNameColor", id is CompanionSummons.SecondBrotherId or CompanionSummons.ThirdBrotherId ? "gold" : "blue");
		if (id != null) result.Add("SummonName", CompanionSummons.GetName(id));
		return result;
	}
}
