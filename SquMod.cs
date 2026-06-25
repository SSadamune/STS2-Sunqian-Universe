using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using Squ.Cards;
using Squ.Character;
using Squ.Relics;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;

namespace Squ;

[ModInitializer(nameof(ModLoaded))]
public static class SquMod
{
	public const string ModId = "sunqian-universe";

	public const string CommonL10nStem = "COMMON";

	public static Logger Logger { get; private set; } = null!;

	public static void ModLoaded()
	{
		var assembly = Assembly.GetExecutingAssembly();

		Logger = RitsuLibFramework.CreateLogger(ModId);
		RegisterCommonLocalization();
		RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
		ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

		RitsuLibFramework.CreateContentPack(ModId)
			.CharacterStarterRelic<SunqianCharacter, BoxLunchRelic>(1)
			.CardKeywordOwnedByLocNamespace("script")
			.ArchaicToothTranscendence<SunQianScript, SunqianUniverse>()
			.Apply();

		new Harmony($"{ModId}.patches").PatchAll(assembly);

		Logger.Info("sunqian-universe (SQU) mod loaded!");
	}

	private static void RegisterCommonLocalization()
	{
		string commonLocRoot = $"{ProfileManager.GetAccountBasePath(ModId)}/localization/common";
		I18N commonL10n = RitsuLibFramework.CreateModLocalization(
			ModId,
			$"{ModId}-common",
			fileSystemFolders: [commonLocRoot],
			pckFolders: [$"res://{ModId}/localization/common"]);

		RitsuLibFramework.RegisterI18NLocTableBridge(ModId, commonL10n, CommonL10nStem);
	}
}
