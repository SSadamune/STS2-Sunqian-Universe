#nullable enable
using System;
using System.Linq;
using MegaCrit.Sts2.Core.Runs;
using Squ.Character;
using Squ.RunData;

namespace Squ.Settings;

/// <summary>
/// Controls whether this mod's registered events are eligible for event selection.
/// Event models remain registered for save compatibility.
/// </summary>
public static class EventRegistrationPolicy
{
	public static bool ShouldInclude(IRunState runState)
	{
		ArgumentNullException.ThrowIfNull(runState);

		return ContentSettingsRunData.EventRegistration switch
		{
			NeutralContentPermissionMode.Allow => true,
			NeutralContentPermissionMode.WhenModCharacterPresent =>
				runState.Players.Any(player => player.Character is SunqianCharacter),
			_ => false,
		};
	}
}
