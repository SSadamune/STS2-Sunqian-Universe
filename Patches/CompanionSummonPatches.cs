#nullable enable
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Combat;

namespace Squ.Patches;

[HarmonyPatch(typeof(OstyCmd), nameof(OstyCmd.Summon))]
internal static class CompanionSummonRedirectPatch
{
	private static bool Prefix(PlayerChoiceContext __0, Player __1, decimal __2,
		AbstractModel? __3, ref Task<SummonResult> __result)
	{
		if (CompanionSummons.ConsumeNativeCall()
			|| CompanionSummons.GetDefaultId(__1) == CompanionSummons.OstyId) return true;
		__result = CompanionSummons.SummonDefault(__0, __1, __2, __3);
		return false;
	}
}

// Vanilla cards retain their plain "Summon X" card text but explain their actual
// bound creature, instead of incorrectly promising Osty in the hover tooltip.
[HarmonyPatch(typeof(HoverTipFactory), nameof(HoverTipFactory.Static))]
internal static class BoundCompanionSummonHoverPatch
{
	private static readonly FieldInfo VarOwner = AccessTools.Field(typeof(DynamicVar), "_owner");
	private static bool Prefix(StaticHoverTip __0, DynamicVar[] __1, ref IHoverTip __result)
	{
		if (__0 != StaticHoverTip.SummonDynamic) return true;
		DynamicVar? amount = __1.FirstOrDefault();
		if (amount == null || VarOwner.GetValue(amount) is not CardModel card
			|| card.IsCanonical || card.Owner == null)
			return true;
		string id = CompanionSummons.GetDefaultId(card.Owner);
		if (id == CompanionSummons.OstyId) return true;
		__result = CompanionSummonKeywords.NamedHoverTip(id, amount);
		return false;
	}
}
