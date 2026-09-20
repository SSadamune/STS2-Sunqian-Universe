using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// 读取本局 <c>RunManager._numReloads</c>（存档字段 num_reloads）。
/// 每次取值时直接读字段，不依赖读档事件缓存。
/// </summary>
public static class RunReloadCount
{
	private static readonly FieldInfo? NumReloadsField =
		AccessTools.Field(typeof(RunManager), "_numReloads");

	public static int Current
	{
		get
		{
			RunManager? manager = RunManager.Instance;
			if (manager == null || NumReloadsField == null)
			{
				return 0;
			}

			return NumReloadsField.GetValue(manager) switch
			{
				int count => count,
				uint count => (int)count,
				long count => (int)count,
				_ => 0,
			};
		}
	}

	public static void Initialize()
	{
		if (NumReloadsField == null)
		{
			SquMod.Logger.Warn("RunManager._numReloads was not found; reload count will be 0.");
		}
	}
}
