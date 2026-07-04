using HarmonyLib;

#nullable enable

namespace Squ.Combat;

internal static class SquStrikeRedirectPatches
{
	private static Harmony? _harmony;
	private static int _patchedMethodCount;

	public static void Initialize(Harmony harmony)
	{
		_harmony = harmony;
		_patchedMethodCount = Patches.BasicStrikeRedirectOnPlayPatch.Apply(harmony);
	}

	public static void EnsureApplied()
	{
		if (_patchedMethodCount > 0 || _harmony == null)
		{
			return;
		}

		_patchedMethodCount = Patches.BasicStrikeRedirectOnPlayPatch.Apply(_harmony);
	}
}
