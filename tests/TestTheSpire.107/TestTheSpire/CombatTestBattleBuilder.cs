using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Rooms;

namespace TestTheSpire;

public sealed class CombatTestBattleBuilder
{
	internal sealed record MonsterFactory(MonsterModel Canonical, Func<MonsterModel> Create);

	internal sealed record PlayerFactory(CharacterModel Character, ulong NetId);

	private readonly List<MonsterFactory> _monsterFactories;

	private readonly List<PlayerFactory> _players;

	private readonly List<PlayerFactory> _extraPlayers;

	private Func<EncounterModel>? _encounterFactory;

	private CharacterModel _playerCharacter;

	private ulong _localNetId;

	private bool _hasExplicitPlayers;

	private bool _removeStartingRelics;

	private RoomType _roomType;

	private string? _seed;

	private bool _useMultiplayerHost;

	public CombatTestBattleBuilder Player<TCharacter>() where TCharacter : CharacterModel
	{
		_playerCharacter = (CharacterModel)(object)ModelDb.Character<TCharacter>();
		return this;
	}

	public CombatTestBattleBuilder Player(CharacterModel character)
	{
		_playerCharacter = character ?? throw new ArgumentNullException("character");
		return this;
	}

	public CombatTestBattleBuilder AddPlayer<TCharacter>(ulong netId) where TCharacter : CharacterModel
	{
		_players.Add(new PlayerFactory((CharacterModel)(object)ModelDb.Character<TCharacter>(), netId));
		_hasExplicitPlayers = true;
		return this;
	}

	public CombatTestBattleBuilder AddPlayer(CharacterModel character, ulong netId)
	{
		_players.Add(new PlayerFactory(character ?? throw new ArgumentNullException("character"), netId));
		_hasExplicitPlayers = true;
		return this;
	}

	public CombatTestBattleBuilder LocalNetId(ulong netId)
	{
		_localNetId = netId;
		return this;
	}

	public CombatTestBattleBuilder AddRemotePlayer<TCharacter>(ulong netId = 2uL) where TCharacter : CharacterModel
	{
		PlayerFactory item = new PlayerFactory((CharacterModel)(object)ModelDb.Character<TCharacter>(), netId);
		if (_hasExplicitPlayers)
		{
			_players.Add(item);
		}
		else
		{
			_extraPlayers.Add(item);
		}
		_useMultiplayerHost = true;
		return this;
	}

	public CombatTestBattleBuilder AddEnemy<TMonster>(Action<TMonster>? configure = null) where TMonster : MonsterModel
	{
		_monsterFactories.Add(new MonsterFactory((MonsterModel)(object)ModelDb.Monster<TMonster>(), delegate
		{
			TMonster val = (TMonster)(object)((MonsterModel)ModelDb.Monster<TMonster>()).ToMutable();
			configure?.Invoke(val);
			return (MonsterModel)(object)val;
		}));
		return this;
	}

	public CombatTestBattleBuilder Encounter<TEncounter>() where TEncounter : EncounterModel
	{
		_encounterFactory = () => ((EncounterModel)ModelDb.Encounter<TEncounter>()).ToMutable();
		return this;
	}

	public CombatTestBattleBuilder Encounter(Func<EncounterModel> encounterFactory)
	{
		_encounterFactory = encounterFactory ?? throw new ArgumentNullException("encounterFactory");
		return this;
	}

	public CombatTestBattleBuilder KeepStartingRelics()
	{
		_removeStartingRelics = false;
		return this;
	}

	public CombatTestBattleBuilder WithSeed(string seed)
	{
		_seed = seed;
		return this;
	}

	public CombatTestBattleBuilder WithRoomType(RoomType roomType)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		_roomType = roomType;
		return this;
	}

	internal CombatTestBattleDefinition Build()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		Func<EncounterModel> encounterFactory = _encounterFactory ?? BuildInlineEncounterFactory();
		PlayerFactory[] array = BuildPlayers();
		ValidatePlayers(array);
		return new CombatTestBattleDefinition(array, _localNetId, _useMultiplayerHost || array.Length > 1, encounterFactory, _roomType, _removeStartingRelics, _seed);
	}

	private PlayerFactory[] BuildPlayers()
	{
		if (_hasExplicitPlayers)
		{
			return _players.ToArray();
		}
		return new PlayerFactory[1]
		{
			new PlayerFactory(_playerCharacter, _localNetId)
		}.Concat(_extraPlayers).ToArray();
	}

	private void ValidatePlayers(IReadOnlyList<PlayerFactory> players)
	{
		if (players.Count == 0)
		{
			throw new InvalidOperationException("At least one player must be configured for a combat test.");
		}
		ulong[] array = (from player in players
			group player by player.NetId into @group
			where @group.Count() > 1
			select @group.Key).ToArray();
		if (array.Length != 0)
		{
			throw new InvalidOperationException("Combat test players must have unique net IDs. Duplicates: " + string.Join(", ", array) + ".");
		}
		if (players.All((PlayerFactory player) => player.NetId != _localNetId))
		{
			throw new InvalidOperationException($"Local net ID {_localNetId} was not found in the configured combat test players.");
		}
	}

	private Func<EncounterModel> BuildInlineEncounterFactory()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		if (_monsterFactories.Count == 0)
		{
			throw new InvalidOperationException("No encounter or monsters were configured for this combat test.");
		}
		MonsterFactory[] factories = _monsterFactories.ToArray();
		RoomType roomType = _roomType;
		return delegate
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			InlineEncounterModel obj = (InlineEncounterModel)(object)((EncounterModel)ModelDb.Encounter<InlineEncounterModel>()).ToMutable();
			obj.Configure(roomType, factories);
			return (EncounterModel)(object)obj;
		};
	}

	public CombatTestBattleBuilder()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		_monsterFactories = new List<MonsterFactory>();
		_players = new List<PlayerFactory>();
		_extraPlayers = new List<PlayerFactory>();
		_playerCharacter = (CharacterModel)(object)ModelDb.Character<Ironclad>();
		_localNetId = 1uL;
		_removeStartingRelics = true;
		_roomType = (RoomType)1;
	}
}
