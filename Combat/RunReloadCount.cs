using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// 缓存本局 <c>RunManager._numReloads</c>（存档字段 num_reloads）。
/// 在读档/开局时读一次，避免调用沉重的 <c>ToSave</c>。
/// </summary>
public static class RunReloadCount
{
	private static readonly FieldInfo? NumReloadsField =
		AccessTools.Field(typeof(RunManager), "_numReloads");

	private static int _cached;
	private static bool _initialized;

	public static int Current => _cached;

	public static void Initialize()
	{
		if (_initialized)
		{
			return;
		}

		_initialized = true;
		RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(_ => RefreshFromRunManager());
		RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(_ => RefreshFromRunManager());
		RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(_ => _cached = 0);
	}

	private static void RefreshFromRunManager()
	{
		RunManager? manager = RunManager.Instance;
		if (manager == null || NumReloadsField == null)
		{
			_cached = 0;
			return;
		}

		_cached = NumReloadsField.GetValue(manager) is int count ? count : 0;
	}
}
