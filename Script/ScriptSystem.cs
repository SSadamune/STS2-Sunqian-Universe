using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using Squ.Cards;
using Squ.Powers;
using Squ.Relics;
using STS2RitsuLib;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Networking.ManagedActions;

#nullable enable

namespace Squ.Script;

public static class ScriptSystem
{
	private readonly record struct BoxLunchReselectionPayload(
		int TurnNumber,
		string[] SelectedRecordIds);

	private const string ScriptCollectionStem = "script_collection";
	public const int ScriptsRequiredForSecondActivationSlot = 4;

	internal const string SelectionPromptKey =
		"SUNQIAN_UNIVERSE_COMMON.SCRIPT.selectionPrompt";
	private static readonly LocString SelectionPrompt =
		new(SquCommonL10n.Table, SelectionPromptKey);
	internal const string PaidSelectionPromptKey =
		"SUNQIAN_UNIVERSE_COMMON.SCRIPT.paidSelectionPrompt";
	internal const string FreeSelectionCountVariable = "FreeSelectionCount";
	internal const string EnergyCostVariable = "EnergyCost";
	internal const string EnergyPrefixVariable = "energyPrefix";
	private static readonly LocString InvalidationPrompt =
		new(SquCommonL10n.Table, "SUNQIAN_UNIVERSE_COMMON.SCRIPT.invalidationPrompt");

	private static bool _initialized;
	private static readonly RitsuLibManagedNetActionDescriptor<BoxLunchReselectionPayload>
		BoxLunchReselectionAction = new(
			SquMod.ModId,
			"box_lunch_script_reselection",
			static payload => JsonSerializer.SerializeToUtf8Bytes(payload),
			static bytes => JsonSerializer.Deserialize<BoxLunchReselectionPayload>(bytes),
			ExecuteSyncedBoxLunchReselectionAsync,
			GameActionType.CombatPlayPhaseOnly);

	public static PileType ScriptCollectionPile { get; private set; }

	public static void RegisterPile()
	{
		ScriptCollectionPile = ModCardPileRegistry.For(SquMod.ModId)
			.RegisterOwned(ScriptCollectionStem, new ModCardPileSpec
			{
				Scope = ModCardPileScope.CombatOnly,
				Style = ModCardPileUiStyle.BottomRight,
				Anchor = new ModCardPileAnchor(
					ModCardPileAnchorKind.BottomRightPrimary,
					new Vector2(100f, -105f)),
				IconPath = "res://images/ui/ScriptCollectionPile.png",
				OnOpen = context =>
				{
					if (context.Player.GetRelic<BoxLunchRelic>() is { } boxLunch
						&& boxLunch.CanReselectFromScriptCollection())
					{
						_ = TaskHelper.RunSafely(boxLunch.ReselectFromScriptCollectionAsync());
						return;
					}

					context.ShowDefaultPileScreen();
				},
				VisibleWhen = context => context.Pile?.Cards.Count > 0,
			}).PileType;
	}

	public static void Initialize()
	{
		if (_initialized)
		{
			return;
		}

		_initialized = true;
		RitsuLibManagedNetActions.Register(BoxLunchReselectionAction);
		RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(
			_ => PlayerScriptLiftTracker.ClearCombat());
		RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(
			_ => PlayerScriptLiftTracker.ClearCombat());
	}

	public static int GetScriptLiftsThisTurn(Player player) =>
		PlayerScriptLiftTracker.GetLiftsThisTurn(player);

	public static bool HasActiveScript(Creature creature) =>
		creature.Powers.OfType<ScriptPowerTemplate>().Any();

	public static IReadOnlyList<ScriptPowerTemplate> GetActiveScripts(Creature creature) =>
		creature.Powers.OfType<ScriptPowerTemplate>().ToList();

	public static bool IsUnlocked<TRecord>(Player player)
		where TRecord : ScriptRecordCardTemplate =>
		ScriptCollectionPile.GetPile(player).Cards.OfType<TRecord>().Any();

	public static int GetActivationLimit(Player player)
	{
		int unlockedCount = ScriptCollectionPile.GetPile(player).Cards
			.OfType<ScriptRecordCardTemplate>()
			.Count();
		int intrinsic = unlockedCount >= ScriptsRequiredForSecondActivationSlot ? 1 : 0;
		int additional = EnumeratePlayerModels(player)
			.OfType<IScriptActivationLimitProvider>()
			.Sum(provider => Math.Max(0, provider.GetAdditionalScriptActivationSlots(player, unlockedCount)));
		return Math.Max(1, 1 + intrinsic + additional);
	}

	public static async Task UnlockOrReinforceAsync<TRecord>(
		PlayerChoiceContext choiceContext,
		ScriptCardTemplate source,
		decimal amount,
		Action<TRecord>? configureRecord = null,
		Action<TRecord>? afterActivation = null)
		where TRecord : ScriptRecordCardTemplate
	{
		Player player = source.Owner;
		CardPile pile = ScriptCollectionPile.GetPile(player);
		TRecord? record = pile.Cards.OfType<TRecord>().FirstOrDefault();
		bool wasNewUnlock = record is null;

		if (record is null)
		{
			ICombatState combatState = player.Creature.CombatState
				?? throw new InvalidOperationException("Scripts can only be unlocked during combat.");
			record = combatState.CreateCard<TRecord>(player);
			record.InitializeFrom(source, amount);
			configureRecord?.Invoke(record);
			await CardPileCmd.AddGeneratedCardToCombat(record, ScriptCollectionPile, player);
			foreach (IAfterScriptUnlocked handler in EnumeratePlayerModels(player).OfType<IAfterScriptUnlocked>())
			{
				await handler.AfterScriptUnlockedAsync(player, record);
			}
		}
		else
		{
			if (record.CanBeReinforced)
			{
				record.ReinforceFrom(source, amount);
			}
			configureRecord?.Invoke(record);
		}

		foreach (IAfterScriptCardPlayed handler in EnumeratePlayerModels(player).OfType<IAfterScriptCardPlayed>())
		{
			await handler.AfterScriptCardPlayedAsync(player, record, wasNewUnlock);
		}

		await ActivatePlayedScriptAsync(
			choiceContext,
			player,
			record);
		afterActivation?.Invoke(record);
	}

	private static async Task ActivatePlayedScriptAsync(
		PlayerChoiceContext choiceContext,
		Player player,
		ScriptRecordCardTemplate record)
	{
		List<ScriptPowerTemplate> activeScripts = player.Creature.Powers
			.OfType<ScriptPowerTemplate>()
			.ToList();
		ScriptPowerTemplate? sameType = activeScripts
			.FirstOrDefault(power => power.GetType() == record.PowerType);
		if (sameType is not null)
		{
			await RemoveForReactivationAsync(sameType, record);
			await record.ActivateAsync(choiceContext, player);
			return;
		}

		int removalCount = Math.Max(
			0,
			activeScripts.Count - GetActivationLimit(player) + 1);
		if (removalCount > 0)
		{
			IReadOnlyList<ScriptPowerTemplate> scriptsToRemove = activeScripts.Count > 1
				? await ChooseScriptsToInvalidateAsync(
					choiceContext,
					player,
					activeScripts,
					removalCount)
				: activeScripts.Take(removalCount).ToList();
			foreach (ScriptPowerTemplate active in scriptsToRemove)
			{
				await RemoveScriptPowerAsync(active);
			}
		}

		await record.ActivateAsync(choiceContext, player);
	}

	private static async Task<IReadOnlyList<ScriptPowerTemplate>> ChooseScriptsToInvalidateAsync(
		PlayerChoiceContext choiceContext,
		Player player,
		IReadOnlyList<ScriptPowerTemplate> activeScripts,
		int selectCount)
	{
		HashSet<Type> activeTypes = activeScripts
			.Select(power => power.GetType())
			.ToHashSet();
		var prefs = new CardSelectorPrefs(InvalidationPrompt, selectCount)
		{
			Cancelable = false,
			RequireManualConfirmation = true,
			PretendCardsCanBePlayed = true,
		};
		List<Type> selectedTypes = (await CardSelectCmd.FromCombatPile(
			choiceContext,
			ScriptCollectionPile.GetPile(player),
			player,
			prefs,
			card => card is ScriptRecordCardTemplate record
				&& activeTypes.Contains(record.PowerType)))
			.OfType<ScriptRecordCardTemplate>()
			.Select(record => record.PowerType)
			.ToList();

		List<ScriptPowerTemplate> selected = activeScripts
			.Where(power => selectedTypes.Contains(power.GetType()))
			.Take(selectCount)
			.ToList();
		if (selected.Count < selectCount)
		{
			selected.AddRange(activeScripts
				.Where(power => !selected.Contains(power))
				.Take(selectCount - selected.Count));
		}

		return selected;
	}

	public static async Task<bool> ChooseActiveScriptsAsync(
		PlayerChoiceContext choiceContext,
		Player player,
		bool cancelable,
		bool requireManualSelection = false)
	{
		IReadOnlyList<ScriptRecordCardTemplate>? selected =
			await ChooseActiveScriptRecordsAsync(
				choiceContext,
				player,
				cancelable,
				requireManualSelection);
		if (selected is null)
		{
			return false;
		}

		await ApplySelectedScriptsAsync(choiceContext, player, selected);
		return true;
	}

	internal static async Task<IReadOnlyList<ScriptRecordCardTemplate>?>
		ChooseActiveScriptRecordsAsync(
			PlayerChoiceContext choiceContext,
			Player player,
			bool cancelable,
			bool requireManualSelection)
	{
		CardPile pile = ScriptCollectionPile.GetPile(player);
		List<ScriptRecordCardTemplate> records = pile.Cards
			.OfType<ScriptRecordCardTemplate>()
			.ToList();
		if (records.Count == 0 || player.Creature.IsDead)
		{
			return null;
		}
		if (records.Count == 1 && !requireManualSelection)
		{
			return records;
		}

		int activationLimit = GetActivationLimit(player);
		int maxSelect = Math.Min(activationLimit, records.Count);
		int minSelect = records.Count < activationLimit ? 1 : maxSelect;
		var prefs = new CardSelectorPrefs(SelectionPrompt, minSelect, maxSelect)
		{
			Cancelable = cancelable,
			RequireManualConfirmation = true,
			PretendCardsCanBePlayed = true,
		};

		List<ScriptRecordCardTemplate> selected = (await CardSelectCmd.FromCombatPile(
			choiceContext,
			pile,
			player,
			prefs,
			card => card is ScriptRecordCardTemplate))
			.OfType<ScriptRecordCardTemplate>()
			.ToList();
		return selected.Count == 0 ? null : selected;
	}

	internal static bool RequestBoxLunchReselection(
		Player player,
		IReadOnlyList<ScriptRecordCardTemplate> selected)
	{
		if (player.PlayerCombatState?.TurnNumber is not int turnNumber
			|| selected.Count == 0)
		{
			return false;
		}

		BoxLunchReselectionPayload payload = new(
			turnNumber,
			selected.Select(record => record.Id.ToString()).ToArray());
		return RitsuLibManagedNetActions.Request(
			RunManager.Instance,
			BoxLunchReselectionAction,
			payload,
			player.NetId);
	}

	private static async Task ExecuteSyncedBoxLunchReselectionAsync(
		RitsuLibManagedNetActionContext<BoxLunchReselectionPayload> context)
	{
		BoxLunchReselectionPayload payload = context.Message;
		Player player = context.Player;
		if (player.GetRelic<BoxLunchRelic>() is not { } boxLunch
			|| player.PlayerCombatState?.TurnNumber != payload.TurnNumber
			|| !CombatManager.Instance.IsPartOfPlayerTurn(player)
			|| player.Creature.IsDead)
		{
			return;
		}

		List<ScriptRecordCardTemplate> records = ScriptCollectionPile.GetPile(player).Cards
			.OfType<ScriptRecordCardTemplate>()
			.ToList();
		int maxSelect = Math.Min(GetActivationLimit(player), records.Count);
		int minSelect = records.Count < GetActivationLimit(player) ? 1 : maxSelect;
		if (payload.SelectedRecordIds.Length < minSelect
			|| payload.SelectedRecordIds.Length > maxSelect
			|| payload.SelectedRecordIds.Distinct(StringComparer.Ordinal).Count()
				!= payload.SelectedRecordIds.Length)
		{
			return;
		}

		Dictionary<string, ScriptRecordCardTemplate> byId = records
			.ToDictionary(record => record.Id.ToString(), StringComparer.Ordinal);
		List<ScriptRecordCardTemplate> selected = [];
		foreach (string id in payload.SelectedRecordIds)
		{
			if (!byId.TryGetValue(id, out ScriptRecordCardTemplate? record))
			{
				return;
			}
			selected.Add(record);
		}

		boxLunch.MarkReselectionUsed(payload.TurnNumber);
		await ApplySelectedScriptsAsync(
			context.PlayerChoiceContext,
			player,
			selected);
	}

	public static async Task<bool> ChooseActiveScriptsWithEnergyAsync(
		PlayerChoiceContext choiceContext,
		Player player,
		bool cancelable)
	{
		CardPile pile = ScriptCollectionPile.GetPile(player);
		List<ScriptRecordCardTemplate> records = pile.Cards
			.OfType<ScriptRecordCardTemplate>()
			.ToList();
		if (records.Count == 0 || player.Creature.IsDead)
		{
			return false;
		}
		if (records.Count == 1)
		{
			await ApplySelectedScriptsAsync(choiceContext, player, records);
			return true;
		}

		int activationLimit = GetActivationLimit(player);
		int freeSelectionCount = Math.Min(activationLimit, records.Count);
		int availableEnergy = Math.Max(0, player.PlayerCombatState?.Energy ?? 0);
		int maxSelect = Math.Min(records.Count, freeSelectionCount + availableEnergy);
		int minSelect = records.Count < activationLimit ? 1 : freeSelectionCount;
		var prefs = new CardSelectorPrefs(
			CreatePaidSelectionPrompt(
				freeSelectionCount,
				energyCost: 0,
				EnergyIconHelper.GetPrefix(records[0])),
			minSelect,
			maxSelect)
		{
			Cancelable = cancelable,
			RequireManualConfirmation = true,
			PretendCardsCanBePlayed = true,
		};

		List<ScriptRecordCardTemplate> selected = (await CardSelectCmd.FromCombatPile(
			choiceContext,
			pile,
			player,
			prefs,
			card => card is ScriptRecordCardTemplate))
			.OfType<ScriptRecordCardTemplate>()
			.ToList();
		if (selected.Count == 0)
		{
			return false;
		}

		int energyCost = Math.Max(0, selected.Count - freeSelectionCount);
		await PlayerCmd.LoseEnergy(energyCost, player);
		await ApplySelectedScriptsAsync(choiceContext, player, selected);
		return true;
	}

	internal static LocString CreatePaidSelectionPrompt(
		int freeSelectionCount,
		int energyCost,
		string energyPrefix)
	{
		LocString prompt = new(SquCommonL10n.Table, PaidSelectionPromptKey);
		prompt.Add(FreeSelectionCountVariable, freeSelectionCount);
		prompt.Add(EnergyCostVariable, energyCost);
		prompt.Add(EnergyPrefixVariable, energyPrefix);
		return prompt;
	}

	private static async Task ApplySelectedScriptsAsync(
		PlayerChoiceContext choiceContext,
		Player player,
		IReadOnlyList<ScriptRecordCardTemplate> selected)
	{
		HashSet<Type> selectedTypes = selected.Select(record => record.PowerType).ToHashSet();
		foreach (ScriptPowerTemplate active in player.Creature.Powers
			.OfType<ScriptPowerTemplate>()
			.Where(power => !selectedTypes.Contains(power.GetType()))
			.ToList())
		{
			await RemoveScriptPowerAsync(active);
		}

		foreach (ScriptRecordCardTemplate record in selected)
		{
			ScriptPowerTemplate? active = player.Creature.Powers
				.OfType<ScriptPowerTemplate>()
				.FirstOrDefault(power => power.GetType() == record.PowerType);
			if (active is not null)
			{
				await RemoveForReactivationAsync(active, record);
				active = null;
			}

			if (active is null)
			{
				await record.ActivateAsync(choiceContext, player);
			}
		}
	}

	private static Task RemoveForReactivationAsync(
		ScriptPowerTemplate active,
		ScriptRecordCardTemplate record) =>
		record.CanBeReinforced
			? RemoveScriptPowerAsync(active, notifyLift: false, resolveEndEffects: false)
			: RemoveScriptPowerAsync(active);

	public static async Task ApplyExternalScriptPowerAsync(
		PlayerChoiceContext choiceContext,
		ScriptPowerTemplate power,
		Creature target,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		bool silent)
	{
		ScriptPowerTemplate? sameType = target.Powers
			.OfType<ScriptPowerTemplate>()
			.FirstOrDefault(active => active.GetType() == power.GetType());
		if (sameType is not null)
		{
			await RemoveScriptPowerAsync(sameType, notifyLift: false, resolveEndEffects: false);
		}

		int limit = target.Player is { } player ? GetActivationLimit(player) : 1;
		while (target.Powers.OfType<ScriptPowerTemplate>().Count() >= limit)
		{
			await RemoveScriptPowerAsync(target.Powers.OfType<ScriptPowerTemplate>().First());
		}

		power.SetActivationChoiceContext(choiceContext);
		await PowerCmd.Apply(choiceContext, power, target, amount, applier, cardSource, silent);
	}

	public static async Task<bool> TryConsumeWrapAsync(
		PlayerChoiceContext choiceContext,
		Creature creature)
	{
		List<ScriptPowerTemplate> activeScripts = creature.Powers
			.OfType<ScriptPowerTemplate>()
			.ToList();
		if (activeScripts.Count == 0)
		{
			return false;
		}

		ScriptPowerTemplate scriptToRemove = activeScripts[0];
		if (activeScripts.Count > 1 && creature.Player is { } player)
		{
			IReadOnlyList<ScriptPowerTemplate> selected =
				await ChooseScriptsToInvalidateAsync(
					choiceContext,
					player,
					activeScripts,
					selectCount: 1);
			scriptToRemove = selected[0];
		}

		await RemoveScriptPowerAsync(scriptToRemove);
		return true;
	}

	public static async Task InvalidateScriptsAsync(Creature creature)
	{
		foreach (ScriptPowerTemplate active in creature.Powers.OfType<ScriptPowerTemplate>().ToList())
		{
			await RemoveScriptPowerAsync(active);
		}
	}

	public static async Task RemoveScriptPowerAsync(
		ScriptPowerTemplate power,
		bool notifyLift = true,
		bool resolveEndEffects = true)
	{
		power.BeginRemoval(notifyLift, resolveEndEffects);

		try
		{
			await PowerCmd.Remove(power);
		}
		finally
		{
			power.EndRemoval(notifyLift, resolveEndEffects);
		}
	}

	internal static async Task NotifyScriptLiftedAsync(Creature creature, PlayerChoiceContext choiceContext)
	{
		Player? player = creature.Player;
		if (player is null)
		{
			return;
		}

		int liftsThisTurn = PlayerScriptLiftTracker.RecordScriptLift(player);
		ScriptLiftContext context = new()
		{
			ChoiceContext = choiceContext,
			Owner = creature,
			LiftsThisTurn = liftsThisTurn,
		};

		foreach (IScriptLiftHandler handler in EnumeratePlayerModels(player)
			.Concat(creature.Powers)
			.OfType<IScriptLiftHandler>())
		{
			await handler.OnScriptLiftAsync(context);
		}
	}

	private static IEnumerable<AbstractModel> EnumeratePlayerModels(Player player)
	{
		foreach (AbstractModel relic in player.Relics)
		{
			yield return relic;
		}
	}
}
