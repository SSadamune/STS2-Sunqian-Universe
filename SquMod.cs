using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;

namespace Squ;

[ModInitializer(nameof(ModLoaded))]
public static class SquMod
{
	public const string ModId = "sunqian-universe";

	public static Logger Logger { get; private set; } = null!;

	public static void ModLoaded()
	{
		var assembly = Assembly.GetExecutingAssembly();

		Logger = RitsuLibFramework.CreateLogger(ModId);
		ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

		Logger.Info("sunqian-universe (SQU) mod loaded!");
	}
}
