#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Squ.Cards;
using Squ.Character;
using Squ.Monsters;
using Squ.Powers;
using STS2RitsuLib;

namespace Squ.Combat;

/// <summary>
/// Fixed character defaults and explicit per-call summons. Explicit and any-choice
/// summons never change the character default. Battle discoveries are owner scoped.
/// </summary>
public static class CompanionSummons
{
	public const string OstyId = "osty";
	public const string SecondBrotherId = "second_brother";

	private sealed record Definition(Type MonsterType, Func<Player, Task<Creature>> Create,
		Func<LocString> Name, string Portrait);
	private sealed class BattleData
	{
		public Dictionary<Player, List<string>> Discovered { get; } = new();
	}
	private static readonly Dictionary<string, Definition> Definitions = new(StringComparer.Ordinal);
	private static readonly Dictionary<Type, string> Defaults = new();
	private static readonly ConditionalWeakTable<ICombatState, BattleData> Battles = new();
	[ThreadStatic] private static bool _nativeCallPending;
	private static bool _initialized;

	public static void Initialize()
	{
		if (_initialized) return;
		_initialized = true;
		Register<Osty>(OstyId, "res://images/cards/CutePet.png");
		Register<SecondBrother>(SecondBrotherId, "res://images/cards/GuanDiForm.png");
		RegisterDefault<LiubeiCharacter>(SecondBrotherId);
		RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(evt =>
		{
			if (evt.CombatState != null) ClearCombat(evt.CombatState);
		});
	}

	/// <summary>The monster type must also be registered with the game's model database.</summary>
	public static void Register<T>(string id, string portraitPath) where T : MonsterModel
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(id);
		ArgumentException.ThrowIfNullOrWhiteSpace(portraitPath);
		if (Definitions.ContainsKey(id)) throw new ArgumentException($"Summon already registered: {id}");
		Definitions.Add(id, new Definition(typeof(T), PlayerCmd.AddPet<T>,
			() => ModelDb.Monster<T>().Title, portraitPath));
	}

	public static void RegisterDefault<TCharacter>(string id) where TCharacter : CharacterModel
	{
		GetDefinition(id);
		Defaults[typeof(TCharacter)] = id;
	}

	public static string GetDefaultId(Player player) =>
		Defaults.GetValueOrDefault(player.Character.GetType(), OstyId);

	public static LocString GetName(string id) => GetDefinition(id).Name();
	public static string GetPortraitPath(string id) => GetDefinition(id).Portrait;

	public static Creature? GetCompanion(Player player, string id)
	{
		Type type = GetDefinition(id).MonsterType;
		return player.Creature.CombatState?.Allies.FirstOrDefault(
			creature => creature.PetOwner == player && creature.Monster?.GetType() == type);
	}

	public static IReadOnlyList<string> GetAvailableIds(Player player)
	{
		List<string> result = [OstyId, GetDefaultId(player)];
		if (player.Creature.CombatState is { } combat
			&& Battles.TryGetValue(combat, out BattleData? data)
			&& data.Discovered.TryGetValue(player, out List<string>? ids))
		{
			result.AddRange(ids);
		}
		return result.Distinct(StringComparer.Ordinal).ToArray();
	}

	internal static void ClearCombat(ICombatState combat) => Battles.Remove(combat);

	/// <summary>Explicit summon, including discovery for this player's any-choice list.</summary>
	public static async Task<SummonResult> SummonSpecific(PlayerChoiceContext context, Player player,
		string id, decimal amount, AbstractModel? source)
	{
		GetDefinition(id);
		SummonResult result = id == OstyId
			? await SummonNative(context, player, amount, source)
			: await SummonCustom(context, player, id, amount, source);
		if (result.Amount > 0 && result.Creature != null)
		{
			Discover(player, id);
		}
		return result;
	}

	/// <summary>Fixed-binding target for ordinary summons, including vanilla cards and relics.</summary>
	public static Task<SummonResult> SummonDefault(PlayerChoiceContext context, Player player,
		decimal amount, AbstractModel? source) =>
		GetDefaultId(player) is var id && id != OstyId
			? SummonCustom(context, player, id, amount, source)
			: SummonNative(context, player, amount, source);

	public static async Task<SummonResult> SummonAny(PlayerChoiceContext context, Player player,
		decimal amount, AbstractModel? source)
	{
		ICombatState combat = RequireCombat(player);
		if (player.Creature.IsDead || CombatManager.Instance.IsOverOrEnding)
		{
			return new SummonResult(null, 0);
		}
		List<CardModel> options = GetAvailableIds(player).Select(id =>
		{
			var card = combat.CreateCard<CompanionSummonChoice>(player);
			card.Configure(id, amount);
			return (CardModel)card;
		}).ToList();
		var prefs = new CardSelectorPrefs(new LocString("card_keywords",
			"SUNQIAN_UNIVERSE_SUMMON.selectionPrompt"), 1)
		{
			Cancelable = false,
			RequireManualConfirmation = true,
			PretendCardsCanBePlayed = true,
			UnpoweredPreviews = true,
		};
		var selection = (await CardSelectCmd.FromSimpleGrid(context, options, player, prefs))
			.OfType<CompanionSummonChoice>().SingleOrDefault();
		return selection == null ? new SummonResult(null, 0)
			: await SummonSpecific(context, player, selection.CompanionId, amount, source);
	}

	private static async Task<SummonResult> SummonCustom(PlayerChoiceContext context, Player player,
		string id, decimal amount, AbstractModel? source)
	{
		Definition definition = GetDefinition(id);
		ICombatState combat = RequireCombat(player);
		Creature? pet = GetCompanion(player, id);
		if (player.Creature.IsDead || CombatManager.Instance.IsOverOrEnding)
		{
			return new SummonResult(pet, 0);
		}
		amount = Hook.ModifySummonAmount(combat, player, amount, source);
		if (amount <= 0) return new SummonResult(pet, 0);
		if (pet?.IsAlive == true)
		{
			await CreatureCmd.GainMaxHp(pet, amount);
		}
		else
		{
			if (pet == null)
			{
				pet = await definition.Create(player);
			}
			else
			{
				player.PlayerCombatState!.AddPetInternal(pet);
			}
			await CreatureCmd.SetMaxHp(pet, amount);
			await CreatureCmd.Heal(pet, amount, playAnim: false);
			if (!pet.HasPower<DieForYouPower>())
			{
				await PowerCmd.Apply<DieForYouPower>(context, pet, 1, null, null);
			}
			NCombatRoom.Instance?.GetCreatureNode(pet)?.TrackBlockStatus(player.Creature);
		}
		if (id == SecondBrotherId && !pet.HasPower<LifeContractPower>())
			await PowerCmd.Apply<LifeContractPower>(context, pet, 1, null, null);
		CombatManager.Instance.History.Summoned(combat, (int)amount, player);
		await Hook.AfterSummon(combat, context, player, amount);
		return new SummonResult(pet, amount);
	}

	// Bypass this mod's redirect for exactly one synchronous entry, preserving
	// other mods' patches and allowing any nested AfterSummon calls to redirect.
	private static Task<SummonResult> SummonNative(PlayerChoiceContext context, Player player,
		decimal amount, AbstractModel? source)
	{
		_nativeCallPending = true;
		try { return OstyCmd.Summon(context, player, amount, source); }
		finally { _nativeCallPending = false; }
	}

	internal static bool ConsumeNativeCall()
	{
		bool pending = _nativeCallPending;
		_nativeCallPending = false;
		return pending;
	}

	private static void Discover(Player player, string id)
	{
		BattleData data = Battles.GetOrCreateValue(RequireCombat(player));
		if (!data.Discovered.TryGetValue(player, out List<string>? ids))
		{
			data.Discovered.Add(player, ids = []);
		}
		if (!ids.Contains(id)) ids.Add(id);
	}

	private static Definition GetDefinition(string id) =>
		Definitions.TryGetValue(id, out Definition? definition) ? definition
			: throw new ArgumentException($"Unknown summon: {id}", nameof(id));
	private static ICombatState RequireCombat(Player player) =>
		player.Creature.CombatState ?? throw new InvalidOperationException("Summoning requires combat.");
}
