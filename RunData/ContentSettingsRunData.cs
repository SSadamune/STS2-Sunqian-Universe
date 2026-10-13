#nullable enable
using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using Squ.Settings;
using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.RunData;

namespace Squ.RunData;

/// <summary>
/// 本局使用的内容设置快照。新开局时由大厅主机写入；读取存档时则重新采用主机此刻的本地设置。
/// </summary>
public static class ContentSettingsRunData
{
	private const string SaveKey = "content_settings";

	private static readonly RunSavedData<State> Saved =
		RitsuLibFramework.GetRunSavedDataStore(SquMod.ModId).Register(
			SaveKey,
			() => new State(),
			new RunSavedDataOptions
			{
				WritePolicy = RunSavedDataWritePolicy.AlwaysWhenRegistered,
			});

	private static bool _initialized;

	public static bool IsReadOnly => RunManager.Instance is { IsInProgress: true };

	public static NeutralContentPermissionMode NeutralPotionModification =>
		GetEffectiveSnapshot().NeutralPotionModification;

	public static NeutralContentPermissionMode NeutralCardRegistration =>
		GetEffectiveSnapshot().NeutralCardRegistration;

	public static void Initialize()
	{
		if (_initialized)
		{
			return;
		}

		_initialized = true;
		RitsuLibFramework.SubscribeLifecycle<RunSavedDataLobbyStagingEvent>(OnLobbyStaging);
		RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(OnRunStarted);
	}

	public static NeutralContentPermissionMode DisplayedPotionMode(SquSettings localSettings) =>
		TryGetActiveSnapshot(out State snapshot)
			? snapshot.NeutralPotionModification
			: NormalizePotionMode(localSettings.NeutralPotionModification);

	public static NeutralContentPermissionMode DisplayedCardMode(SquSettings localSettings) =>
		TryGetActiveSnapshot(out State snapshot)
			? snapshot.NeutralCardRegistration
			: NormalizeCardMode(localSettings.NeutralCardRegistration);

	public static bool TryWriteLocalPotionMode(
		SquSettings localSettings,
		NeutralContentPermissionMode value)
	{
		if (IsReadOnly)
		{
			return false;
		}

		localSettings.NeutralPotionModification = NormalizePotionMode(value);
		return true;
	}

	public static bool TryWriteLocalCardMode(
		SquSettings localSettings,
		NeutralContentPermissionMode value)
	{
		if (IsReadOnly)
		{
			return false;
		}

		localSettings.NeutralCardRegistration = NormalizeCardMode(value);
		return true;
	}

	internal static void RefreshLoadedSaveSnapshot(SerializableRun save)
	{
		try
		{
			Type? registryType = AccessTools.TypeByName("STS2RitsuLib.RunData.RunSavedDataRegistry");
			MethodInfo? buildPayload = AccessTools.Method(
				registryType,
				"BuildPayloadFromSerializable",
				[typeof(SerializableRun)]);
			MethodInfo? attachDocument = AccessTools.Method(
				registryType,
				"AttachDocumentFromJson",
				[typeof(SerializableRun), typeof(string)]);
			if (buildPayload is null || attachDocument is null)
			{
				SquMod.Logger.Warn(
					"[ContentSettings] RitsuLib RunSavedData serializer was not found; " +
					"the loaded-run snapshot could not be refreshed.");
				return;
			}

			string? payload = buildPayload.Invoke(null, [save]) as string;
			JsonObject root = string.IsNullOrWhiteSpace(payload)
				? new JsonObject()
				: JsonNode.Parse(payload)?.AsObject() ?? new JsonObject();
			JsonObject ritsuRoot = GetOrAddObject(root, "_ritsulib");
			ritsuRoot["version"] = 1;
			JsonObject runSavedData = GetOrAddObject(ritsuRoot, "run_saved_data");
			JsonObject modData = GetOrAddObject(runSavedData, SquMod.ModId);
			modData[SaveKey] = new JsonObject
			{
				["schema"] = 1,
				["kind"] = "run",
				["data"] = JsonSerializer.SerializeToNode(CreateLocalSnapshot()),
			};

			attachDocument.Invoke(null, [save, root.ToJsonString()]);
		}
		catch (Exception ex)
		{
			SquMod.Logger.Warn(
				"[ContentSettings] Failed to refresh the loaded-run settings snapshot: " +
				ex.GetBaseException().Message);
		}
	}

	internal static void RefreshLoadedRunSnapshot(RunState runState)
	{
		Saved.Set(runState, CreateLocalSnapshot());
	}

	private static void OnLobbyStaging(RunSavedDataLobbyStagingEvent evt)
	{
		if (!evt.IsMultiplayer || evt.IsHost)
		{
			Saved.Lobby.Set(evt.Lobby, CreateLocalSnapshot());
		}
	}

	private static void OnRunStarted(RunStartedEvent evt)
	{
		if (!Saved.TryGet(evt.RunState, out _)
			&& RunManager.Instance.NetService.Type is NetGameType.Singleplayer or NetGameType.Host)
		{
			Saved.Set(evt.RunState, CreateLocalSnapshot());
		}
	}

	private static State GetEffectiveSnapshot()
	{
		if (TryGetActiveSnapshot(out State snapshot))
		{
			return snapshot;
		}

		return CreateLocalSnapshot();
	}

	private static bool TryGetActiveSnapshot(out State snapshot)
	{
		RunState? runState = RunManager.Instance.DebugOnlyGetState();
		if (runState is not null)
		{
			snapshot = Saved.Get(runState);
			Normalize(snapshot);
			return true;
		}

		snapshot = null!;
		return false;
	}

	private static State CreateLocalSnapshot()
	{
		SquSettings settings = ModDataStore.For(SquMod.ModId)
			.Get<SquSettings>(SquSettings.DataKey);
		return new State
		{
			NeutralPotionModification = NormalizePotionMode(settings.NeutralPotionModification),
			NeutralCardRegistration = NormalizeCardMode(settings.NeutralCardRegistration),
		};
	}

	private static void Normalize(State snapshot)
	{
		snapshot.NeutralPotionModification =
			NormalizePotionMode(snapshot.NeutralPotionModification);
		snapshot.NeutralCardRegistration =
			NormalizeCardMode(snapshot.NeutralCardRegistration);
	}

	private static NeutralContentPermissionMode NormalizePotionMode(
		NeutralContentPermissionMode value) =>
		Enum.IsDefined(value)
			? value
			: SquSettings.DefaultNeutralPotionModificationMode;

	private static NeutralContentPermissionMode NormalizeCardMode(
		NeutralContentPermissionMode value) =>
		Enum.IsDefined(value)
			? value
			: SquSettings.DefaultNeutralCardRegistrationMode;

	private static JsonObject GetOrAddObject(JsonObject parent, string propertyName)
	{
		if (parent[propertyName] is JsonObject existing)
		{
			return existing;
		}

		JsonObject created = new();
		parent[propertyName] = created;
		return created;
	}

	public sealed class State
	{
		public NeutralContentPermissionMode NeutralPotionModification { get; set; } =
			SquSettings.DefaultNeutralPotionModificationMode;

		public NeutralContentPermissionMode NeutralCardRegistration { get; set; } =
			SquSettings.DefaultNeutralCardRegistrationMode;
	}
}

/// <summary>
/// RitsuLib 在多人读取存档时从主机的 <see cref="SerializableRun"/> 同步 Run Data。
/// 在大厅建立之初替换内容设置槽位，确保客户端收到的是主机当前设置，而非上次保存时的旧快照。
/// </summary>
[HarmonyPatch(
	typeof(LoadRunLobby),
	MethodType.Constructor,
	[typeof(INetGameService), typeof(ILoadRunLobbyListener), typeof(SerializableRun)])]
internal static class ContentSettingsLoadedRunPatch
{
	private static void Postfix(INetGameService netService, SerializableRun runSave)
	{
		if (netService.Type is NetGameType.Singleplayer or NetGameType.Host
			&& !RunManager.Instance.IsInProgress)
		{
			ContentSettingsRunData.RefreshLoadedSaveSnapshot(runSave);
		}
	}
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpSavedSingleplayer))]
internal static class ContentSettingsLoadedSingleplayerPatch
{
	private static void Prefix(RunState state)
	{
		ContentSettingsRunData.RefreshLoadedRunSnapshot(state);
	}
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpSavedMultiplayer))]
internal static class ContentSettingsLoadedMultiplayerPatch
{
	private static void Prefix(RunState state, LoadRunLobby lobby)
	{
		if (lobby.NetService.Type == NetGameType.Host)
		{
			ContentSettingsRunData.RefreshLoadedRunSnapshot(state);
		}
	}
}
