using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using Squ.Cards;
using Squ.Relics;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// Only the owned relic inside an active run lists the Family effects recorded this run.
/// Canonical hover tips used by the event and relic library keep the general description.
/// </summary>
[HarmonyPatch(typeof(RelicModel), "get_DynamicDescription")]
internal static class NeverHadTheseRelicDescriptionPatch
{
	private const string TriggeredEffectsLineVar = "TriggeredEffectsLine";

	private static void Postfix(RelicModel __instance, LocString __result)
	{
		if (__instance is not NeverHadTheseRelic relic)
		{
			return;
		}

		__result.Add(TriggeredEffectsLineVar, string.Empty);
		if (!ShouldShowTriggeredEffects(relic))
		{
			return;
		}

		LocString line = new("relics", relic.Id.Entry + ".triggeredEffectsLine");
		AddOptionalEffect(
			line,
			"CutePetEffect",
			relic.Id.Entry + ".cutePetEffect",
			relic.RemovedCutePets,
			effect => effect.Add("Summon", CutePet.SummonAmount * relic.RemovedCutePets));
		AddOptionalEffect(
			line,
			"BelovedPartnerEffect",
			relic.Id.Entry + ".belovedPartnerEffect",
			relic.RemovedBelovedPartners,
			effect =>
			{
				effect.Add("Regen", BelovedPartner.RegenAmount * relic.RemovedBelovedPartners);
				effect.Add("Tinder", BelovedPartner.TinderAmount * relic.RemovedBelovedPartners);
			});
		AddOptionalEffect(
			line,
			"NextOfKinEffect",
			relic.Id.Entry + ".nextOfKinEffect",
			relic.RemovedNextOfKin,
			effect => effect.Add("Cards", NextOfKin.DrawAmount * relic.RemovedNextOfKin));
		SquKeywords.AddNestedLoc(__result, TriggeredEffectsLineVar, line);
	}

	private static bool ShouldShowTriggeredEffects(NeverHadTheseRelic relic) =>
		relic.IsMutable
		&& relic.Owner is not null
		&& relic.RemovedFamilyCards > 0
		&& RunManager.Instance is { IsInProgress: true };

	private static void AddOptionalEffect(
		LocString parent,
		string variable,
		string localizationKey,
		int removedCount,
		Action<LocString> addDynamicValues)
	{
		if (removedCount <= 0)
		{
			parent.Add(variable, string.Empty);
			return;
		}

		LocString effect = new("relics", localizationKey);
		addDynamicValues(effect);
		SquKeywords.AddNestedLoc(parent, variable, effect);
	}
}
