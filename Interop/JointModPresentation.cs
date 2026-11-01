using System;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

#nullable enable

namespace Squ.Interop;

internal enum JointModPresentationMode
{
	None,
	HoverOnly,
	InlineKeywords,
}

/// <summary>
/// Resolves the shared A/B/C presentation rules for optional-mod card variants.
/// Gameplay interop remains owner/target based; only presentation uses the local player.
/// </summary>
internal static class JointModPresentation
{
	public static JointModPresentationMode Resolve(
		bool isModReady,
		Func<CharacterModel?, bool> isJointModCharacter)
	{
		RunState? runState = RunManager.Instance.DebugOnlyGetState();
		if (runState is null)
		{
			return Resolve(
				isModReady,
				isInRun: false,
				isInCombat: false,
				partyContainsJointModCharacter: false,
				localPlayerIsJointModCharacter: false);
		}

		bool partyContainsJointModCharacter = runState.Players.Any(player =>
			isJointModCharacter(player.Character));
		bool localPlayerIsJointModCharacter = isJointModCharacter(
			LocalContext.GetMe(runState)?.Character);

		return Resolve(
			isModReady,
			isInRun: true,
			CombatManager.Instance is { IsInProgress: true },
			partyContainsJointModCharacter,
			localPlayerIsJointModCharacter);
	}

	internal static JointModPresentationMode Resolve(
		bool isModReady,
		bool isInRun,
		bool isInCombat,
		bool partyContainsJointModCharacter,
		bool localPlayerIsJointModCharacter)
	{
		if (!isModReady)
		{
			return JointModPresentationMode.None;
		}

		if (!isInRun)
		{
			return JointModPresentationMode.HoverOnly;
		}

		if (!partyContainsJointModCharacter)
		{
			return JointModPresentationMode.None;
		}

		if (!isInCombat)
		{
			return JointModPresentationMode.HoverOnly;
		}

		return localPlayerIsJointModCharacter
			? JointModPresentationMode.InlineKeywords
			: JointModPresentationMode.HoverOnly;
	}
}
