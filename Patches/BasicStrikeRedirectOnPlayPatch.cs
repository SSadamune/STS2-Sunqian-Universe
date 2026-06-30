using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Squ.Combat;

#nullable enable

namespace Squ.Patches;

/// <summary>
/// Prefixes basic <see cref="CardTag.Strike"/> <c>OnPlay</c> overrides for vanilla/basic strikes.
/// </summary>
internal static class BasicStrikeRedirectOnPlayPatch
{
	private static readonly Type[] OnPlayParameterTypes =
		[typeof(PlayerChoiceContext), typeof(CardPlay)];

	public static int Apply(Harmony harmony)
	{
		var prefix = new HarmonyMethod(typeof(BasicStrikeRedirectOnPlayPatch), nameof(Prefix));
		int patched = 0;

		foreach (MethodInfo onPlay in EnumerateBasicStrikeOnPlayMethods())
		{
			try
			{
				harmony.Patch(onPlay, prefix: prefix);
				patched++;
				SquMod.Logger?.Info(
					$"Patched basic strike OnPlay: {onPlay.DeclaringType?.FullName}");
			}
			catch (Exception ex)
			{
				SquMod.Logger?.Warn(
					$"Failed to patch basic strike OnPlay on {onPlay.DeclaringType?.Name}: {ex.Message}");
			}
		}

		SquMod.Logger?.Info($"Patched {patched} basic strike OnPlay methods for chicken-foot-cheese.");
		return patched;
	}

	private static IEnumerable<MethodInfo> EnumerateBasicStrikeOnPlayMethods()
	{
		var seen = new HashSet<MethodInfo>();

		foreach (CardModel card in ModelDb.AllCards)
		{
			if (card.Rarity != CardRarity.Basic || !card.Tags.Contains(CardTag.Strike))
			{
				continue;
			}

			MethodInfo? onPlay = AccessTools.DeclaredMethod(card.GetType(), "OnPlay", OnPlayParameterTypes)
				?? AccessTools.Method(card.GetType(), "OnPlay", OnPlayParameterTypes);
			if (onPlay == null || !seen.Add(onPlay))
			{
				continue;
			}

			yield return onPlay;
		}
	}

	private static bool Prefix(
		CardModel __instance,
		PlayerChoiceContext choiceContext,
		CardPlay cardPlay,
		ref Task __result)
	{
		if (!SquBasicStrikeRedirect.ShouldHandleInOnPlay(__instance))
		{
			return true;
		}

		__result = SquBasicStrikeRedirect.ExecuteRedirectedBasicStrikeDamage(__instance, choiceContext);
		return false;
	}
}
