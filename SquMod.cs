using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using Squ.Audio;
using Squ.Cards;
using Squ.Character;
using Squ.Combat;
using Squ.Interop;
using Squ.Relics;
using Squ.RunData;
using Squ.Script;
using Squ.Settings;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models.Capabilities;
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
		SquTargetTypes.Register();
		RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
		ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
		SunqianSelectBgm.Register();
		SquSfx.Register();
		SquSettingsPage.Register();

		RitsuLibFramework.CreateContentPack(ModId)
			.CharacterStarterRelic<SunqianCharacter, BoxLunchRelic>(1)
			.CardKeywordOwnedByLocNamespace("script")
			.CardKeywordOwnedByLocNamespace("doom_kill_threshold")
			.CardKeywordOwnedByLocNamespace("stackable_script")
			.CardKeywordOwnedByLocNamespace("multi_target")
			.CardKeywordOwnedByLocNamespace("environmental")
			.CardKeywordOwnedByLocNamespace("scry")
			.CardKeywordOwnedByLocNamespace("counts_as_played")
			.CardKeywordOwnedByLocNamespace("charge")
			.CardKeywordOwnedByLocNamespace("wrap")
			.CardKeywordOwnedByLocNamespace("strong_monster_encounter")
			.CardKeywordOwnedByLocNamespace("slight_revision")
			.CardKeywordOwnedByLocNamespace(
				"enthralled",
				iconPath: null,
				ModKeywordCardDescriptionPlacement.BeforeCardDescription,
				includeInCardHoverTip: true)
			.CardKeywordOwnedByLocNamespace(
				"eunuch_message",
				iconPath: null,
				ModKeywordCardDescriptionPlacement.BeforeCardDescription,
				includeInCardHoverTip: true)
			.CardKeywordOwnedByLocNamespace(
				"fit",
				iconPath: null,
				ModKeywordCardDescriptionPlacement.AfterCardDescription,
				includeInCardHoverTip: true)
			.CardKeywordOwnedByLocNamespace(
				"war_feeds_war",
				iconPath: null,
				ModKeywordCardDescriptionPlacement.AfterCardDescription,
				includeInCardHoverTip: false)
			.ArchaicToothTranscendence<SunqianScript, SunqianUniverse>()
			.TouchOfOrobasRefinement<BoxLunchRelic, AbundantBoxLunchRelic>()
			.Apply();

		CardDrawPlayRateTracker.Initialize();
		AllInResolutionTracker.Initialize();
		RitsuLibFramework.RegisterModelCapability<SlightRevisionCapability>(ModId);
		RitsuLibFramework.RegisterModelCapability<NightRaidWuchaoStrikeCapability>(ModId);
		RunReloadCount.Initialize();
		DailyWageRunData.Initialize();
		ScriptSystem.Initialize();
		SlightRevisionSystem.Initialize();
		NightRaidWuchaoStrikeSystem.Initialize();
		RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(_ => WarFeedsWarResolutionTracker.ClearCombat());

		var harmony = new Harmony($"{ModId}.patches");
		PatchAllResilient(harmony, assembly);
		SquStrikeRedirectPatches.Initialize(harmony);

		RitsuLibFramework.SubscribeLifecycleOnce<DeferredInitializationCompletedEvent>(_ =>
			NewsanguoInteropDiagnostics.LogSnapshot("DeferredInitializationCompleted"));

		Logger.Info("sunqian-universe (SQU) mod loaded!");
		NewsanguoInteropDiagnostics.LogSnapshot("ModLoaded");
	}

	private static void PatchAllResilient(Harmony harmony, Assembly assembly)
	{
		foreach (Type type in AccessTools.GetTypesFromAssembly(assembly))
		{
			if (!HasHarmonyPatchAnnotations(type))
			{
				continue;
			}

			try
			{
				harmony.CreateClassProcessor(type).Patch();
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to apply Harmony patches on {type.FullName}: {ex}");
			}
		}
	}

	/// <summary>
	/// Harmony 会把名为 Prefix/Postfix 的方法当成补丁入口。
	/// 手动 <c>harmony.Patch</c> 的类型没有 <see cref="HarmonyPatch"/>，必须跳过，否则会报 Undefined target method。
	/// </summary>
	private static bool HasHarmonyPatchAnnotations(Type type)
	{
		if (type.GetCustomAttributes(true).OfType<HarmonyAttribute>().Any())
		{
			return true;
		}

		return AccessTools.GetDeclaredMethods(type)
			.Any(method => method.GetCustomAttributes(true).OfType<HarmonyAttribute>().Any());
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
