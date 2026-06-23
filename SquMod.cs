using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using Squ.Character;
using Squ.Relics;
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
			.CharacterStarterRelic<SunqianCharacter, BoxLunchRelic>(1)
			.CardKeywordOwnedByLocNamespace("script")
			.Apply();

		Logger.Info("sunqian-universe (SQU) mod loaded!");
	}
}
