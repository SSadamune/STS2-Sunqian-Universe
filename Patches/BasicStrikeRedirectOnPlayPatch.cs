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
		var prefix = new HarmonyMethod(typeof(BasicStrikeRedirectOnPlayPatch), nameof(BeginRedirectOnPlay));
		var postfix = new HarmonyMethod(typeof(BasicStrikeRedirectOnPlayPatch), nameof(CompleteRedirectOnPlay));
		var finalizer = new HarmonyMethod(typeof(BasicStrikeRedirectOnPlayPatch), nameof(CleanupFailedOnPlay));
		int patched = 0;

		foreach (MethodInfo onPlay in EnumerateBasicStrikeOnPlayMethods())
		{
			try
			{
				harmony.Patch(onPlay, prefix: prefix, postfix: postfix, finalizer: finalizer);
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

	private static void BeginRedirectOnPlay(
		CardModel __instance,
		CardPlay cardPlay,
		out SquBasicStrikeRedirect.RedirectScope? __state)
	{
		if (!SquBasicStrikeRedirect.ShouldHandleInOnPlay(__instance))
		{
			__state = null;
			return;
		}

		__state = SquBasicStrikeRedirect.BeginRedirectedBasicStrike(__instance, cardPlay);
	}

	private static void CompleteRedirectOnPlay(
		PlayerChoiceContext choiceContext,
		SquBasicStrikeRedirect.RedirectScope? __state,
		ref Task __result)
	{
		if (__state is not null)
		{
			__result = SquBasicStrikeRedirect.CompleteRedirectedBasicStrike(
				__result,
				__state,
				choiceContext);
		}
	}

	private static Exception? CleanupFailedOnPlay(
		Exception? __exception,
		SquBasicStrikeRedirect.RedirectScope? __state)
	{
		if (__exception is not null && __state is not null)
		{
			SquBasicStrikeRedirect.AbortRedirectedBasicStrike(__state);
		}

		return __exception;
	}
}
