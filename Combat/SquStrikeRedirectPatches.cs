using HarmonyLib;
using STS2RitsuLib;

#nullable enable

namespace Squ.Combat;

internal static class SquStrikeRedirectPatches
{
	private static Harmony? _harmony;
	private static int _patchedMethodCount;
	private static bool _lifecycleSubscribed;

	public static void Initialize(Harmony harmony)
	{
		_harmony = harmony;

		// ModLoaded is too early for ModelDb.AllCards (character loc keys may be missing).
		// Apply once deferred init finishes; ChickenFootCheeseStrikePower also calls EnsureApplied.
		if (!_lifecycleSubscribed)
		{
			_lifecycleSubscribed = true;
			RitsuLibFramework.SubscribeLifecycleOnce<DeferredInitializationCompletedEvent>(_ =>
				EnsureApplied());
		}
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
