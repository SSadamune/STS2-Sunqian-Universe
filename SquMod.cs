using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.Relics;
using Squ.Character;
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
		RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
		ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

		RitsuLibFramework.CreateContentPack(ModId)
			.CharacterStarterRelic<SunqianCharacter, Akabeko>(1)
			.Apply();

		Logger.Info("sunqian-universe (SQU) mod loaded!");
	}
}
