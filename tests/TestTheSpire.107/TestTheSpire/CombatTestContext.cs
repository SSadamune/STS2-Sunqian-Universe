using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

namespace TestTheSpire;

public sealed class CombatTestContext
{
	private sealed class CombatTestNoOpAction : GameAction
	{
		public override ulong OwnerId { get; }

		public override GameActionType ActionType => (GameActionType)4;

		public CombatTestNoOpAction(ulong ownerId)
		{
			OwnerId = ownerId;
		}

		protected override Task ExecuteAction()
		{
			return Task.CompletedTask;
		}

		public override INetAction ToNetAction()
		{
			throw new NotSupportedException("CombatTestNoOpAction is only used inside the local test runner.");
		}

		public override string ToString()
		{
			return $"{"CombatTestNoOpAction"} owner {((GameAction)this).OwnerId}";
		}
	}

	private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(30L);

	private static readonly FieldInfo? CombatStateChangedField = typeof(CombatStateTracker).GetField("CombatStateChanged", BindingFlags.Instance | BindingFlags.NonPublic);

	private readonly NGame _game;

	private Action<GameAction>? _trackChecksumActionStart;

	private Action<GameAction>? _generateMissingPostActionChecksum;

	private Action<GameAction>? _trackExecutedAction;

	private bool _checksumTrackingFallbackInstalled;

	private bool _actionExecutionTrackingInstalled;

	private int _executedActionCount;

	private uint _checksumNextIdAtActionStart;

	public Player Player { get; }

	public IReadOnlyList<Player> Players { get; }

	public RunState RunState { get; }

	public CombatState CombatState { get; }

	public IReadOnlyList<Creature> Enemies => CombatState.Enemies;

	public int ExecutedActionCount => _executedActionCount;

	private CombatTestContext(NGame game, Player player, IReadOnlyList<Player> players, RunState runState, CombatState combatState)
	{
		_game = game;
		Player = player;
		Players = players;
		RunState = runState;
		CombatState = combatState;
	}

	internal static async Task<CombatTestContext> CreateAsync(NGame game, CombatTestBattleDefinition definition, string seed)
	{
		await ResetEnvironmentAsync(game);
		TestMode.TurnOnInternal();
		game.StartOnMainMenu = false;
		Player[] players = definition.Players.Select((CombatTestBattleBuilder.PlayerFactory playerFactory) => Player.CreateForNewRun(playerFactory.Character, UnlockState.all, playerFactory.NetId)).ToArray();
		Player player = players.Single((Player p) => p.NetId == definition.LocalNetId);
		if (definition.RemoveStartingRelics)
		{
			Player[] array = players;
			foreach (Player val in array)
			{
				foreach (RelicModel item in val.Relics.ToList())
				{
					val.RemoveRelicInternal(item, true);
				}
			}
		}
		RunState runState = RunState.CreateForTest((IReadOnlyList<Player>)players, (IReadOnlyList<ActModel>)null, (IReadOnlyList<ModifierModel>)null, (GameMode)1, 0, seed);
		INetGameService obj;
		if (!definition.UseMultiplayerHost)
		{
			INetGameService val2 = (INetGameService)new NetSingleplayerGameService();
			obj = val2;
		}
		else
		{
			INetGameService val2 = (INetGameService)(object)new TestNetGameService(definition.LocalNetId, (NetGameType)2);
			obj = val2;
		}
		INetGameService val3 = obj;
		RunManager.Instance.SetUpTest(runState, val3, true, false);
		RunManager.Instance.GenerateRooms();
		await PreloadManager.LoadRunAssets(runState.Players.Select((Player p) => p.Character));
		await PreloadManager.LoadActAssets(runState.Act);
		await RunManager.Instance.FinalizeStartingRelics();
		RunManager.Instance.Launch();
		game.RootSceneContainer.SetCurrentScene((Control)(object)NRun.Create(runState));
		await WaitForStatic(() => NRun.Instance != null, "Run scene did not initialize.");
		EncounterModel val4 = definition.EncounterFactory();
		await RunManager.Instance.EnterRoomDebug(definition.RoomType, (MapPointType)0, (AbstractModel)(object)val4, false);
		await WaitForStatic(delegate
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Invalid comparison between Unknown and I4
			if (CombatManager.Instance.IsInProgress)
			{
				PlayerCombatState playerCombatState = player.PlayerCombatState;
				if (playerCombatState == null)
				{
					return false;
				}
				return (int)playerCombatState.Phase == 3;
			}
			return false;
		}, "Combat play phase did not begin.");
		game.SetScreenShakeTarget((Control)(object)game.RootSceneContainer);
		CombatState combatState = CombatManager.Instance.DebugOnlyGetState() ?? throw new InvalidOperationException("Combat state was not available.");
		return new CombatTestContext(game, player, players, runState, combatState);
	}

	public Player PlayerWithNetId(ulong netId)
	{
		return Players.Single((Player player) => player.NetId == netId);
	}

	public void EnableChecksumTracking()
	{
		ChecksumTracker checksumTracker = RunManager.Instance.ChecksumTracker;
		PropertyInfo property = ((object)checksumTracker).GetType().GetProperty("IsEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if ((object)property != null && property.CanWrite && property.PropertyType == typeof(bool))
		{
			property.SetValue(checksumTracker, true);
		}
		EnsurePostActionChecksumFallback();
	}

	public void BeginActionExecutionTracking()
	{
		RemoveActionExecutionTracking();
		_executedActionCount = 0;
		_trackExecutedAction = delegate
		{
			_executedActionCount++;
		};
		RunManager.Instance.ActionExecutor.AfterActionExecuted += _trackExecutedAction;
		_actionExecutionTrackingInstalled = true;
	}

	public void EndActionExecutionTracking()
	{
		RemoveActionExecutionTracking();
	}

	public Creature EnemyAt(int index)
	{
		if (index < 0 || index >= Enemies.Count)
		{
			throw new ArgumentOutOfRangeException("index", $"Enemy index {index} is out of range.");
		}
		return Enemies[index];
	}

	public Creature Enemy<TMonster>() where TMonster : MonsterModel
	{
		List<Creature> list = Enemies.Where((Creature enemy) => enemy.Monster is TMonster).ToList();
		return (Creature)(list.Count switch
		{
			1 => list[0], 
			0 => throw new InvalidOperationException("No enemy of type " + typeof(TMonster).Name + " was found."), 
			_ => throw new InvalidOperationException($"Expected exactly one enemy of type {typeof(TMonster).Name}, found {list.Count}."), 
		});
	}

	public async Task<TCard> AddToHand<TCard>() where TCard : CardModel
	{
		return await AddToHand<TCard>(Player);
	}

	public async Task<TCard> AddToHand<TCard>(Player owner) where TCard : CardModel
	{
		TCard card = CombatState.CreateCard<TCard>(owner);
		await CardPileCmd.AddGeneratedCardToCombat((CardModel)(object)card, (PileType)2, owner, (CardPilePosition)1);
		await WaitFor(() => PileTypeExtensions.GetPile((PileType)2, owner).Cards.Contains((CardModel)(object)card), "Injected " + typeof(TCard).Name + " did not appear in hand.");
		return card;
	}

	public async Task Play(CardModel card, Creature? target = null)
	{
		if (!card.TryManualPlay(target))
		{
			throw new InvalidOperationException("Failed to play " + ((AbstractModel)card).Id.Entry + ".");
		}
		await WaitFor(() => !PileTypeExtensions.GetPile((PileType)2, card.Owner).Cards.Contains(card), ((AbstractModel)card).Id.Entry + " never left the hand.");
		await WaitForIdle();
	}

	public async Task PlayNetworkAction(CardModel card, Creature? target = null)
	{
		await EnqueueNetworkAction((GameAction)new PlayCardAction(card, target));
		await WaitFor(() => !PileTypeExtensions.GetPile((PileType)2, card.Owner).Cards.Contains(card), ((AbstractModel)card).Id.Entry + " never left the hand.");
	}

	public async Task EnqueueNetworkAction(GameAction action)
	{
		RunManager.Instance.ActionQueueSet.EnqueueWithoutSynchronizing(action);
		await WaitForIdle();
	}

	public async Task EnsureActionExecutedForCleanupAsync(string displayName)
	{
		if (ExecutedActionCount <= 0)
		{
			Log.Info($"[{CombatTestBootstrap.LogPrefix}] {displayName} completed without a GameAction; " + "enqueueing cleanup no-op action.", 2);
			await EnqueueNetworkAction((GameAction)(object)new CombatTestNoOpAction(Player.NetId));
		}
	}

	public async Task EndPlayerTurn()
	{
		if ((int)CombatState.CurrentSide != 1)
		{
			throw new InvalidOperationException("Cannot end the player turn while it is not the player's turn.");
		}
		int currentRound = CombatState.RoundNumber;
		PlayerCmd.EndTurn(Player, false, (Func<Task>)null);
		await WaitFor(delegate
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Invalid comparison between Unknown and I4
			return (int)CombatState.CurrentSide == 1 && CombatState.RoundNumber > currentRound;
		}, "The next player turn did not start in time.");
		await WaitForIdle();
	}

	public async Task<TPower?> ApplyPower<TPower>(Creature target, int amount, Creature? applier = null, CardModel? cardSource = null) where TPower : PowerModel
	{
		TPower power = await PowerCmd.Apply<TPower>((PlayerChoiceContext)new BlockingPlayerChoiceContext(), target, (decimal)amount, applier, cardSource, true);
		await WaitForIdle();
		return power;
	}

	public Task WaitFor(Func<bool> condition, string timeoutMessage)
	{
		return WaitForStatic(condition, timeoutMessage);
	}

	public async Task WaitForIdle()
	{
		await NextFrame();
		await WaitFor(() => !RunManager.Instance.ActionExecutor.IsRunning, "Action queue did not finish.");
		await NextFrame();
	}

	public async Task ResetAsync()
	{
		RemoveActionExecutionTracking();
		RemovePostActionChecksumFallback();
		await ResetEnvironmentAsync(_game);
	}

	private void EnsurePostActionChecksumFallback()
	{
		if (_checksumTrackingFallbackInstalled)
		{
			return;
		}
		ActionExecutor actionExecutor = RunManager.Instance.ActionExecutor;
		_trackChecksumActionStart = delegate
		{
			_checksumNextIdAtActionStart = RunManager.Instance.ChecksumTracker.NextId;
		};
		_generateMissingPostActionChecksum = delegate(GameAction action)
		{
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			if (CombatManager.Instance.IsInProgress)
			{
				bool flag = ((action is EndPlayerTurnAction || action is ReadyToBeginEnemyTurnAction) ? true : false);
				if (!flag && RunManager.Instance.ChecksumTracker.NextId == _checksumNextIdAtActionStart)
				{
					RunManager.Instance.ChecksumTracker.GenerateChecksum($"finished action execution {action}", action);
				}
			}
		};
		actionExecutor.BeforeActionExecuted += _trackChecksumActionStart;
		actionExecutor.AfterActionExecuted += _generateMissingPostActionChecksum;
		_checksumTrackingFallbackInstalled = true;
	}

	private void RemovePostActionChecksumFallback()
	{
		if (_checksumTrackingFallbackInstalled)
		{
			RunManager instance = RunManager.Instance;
			if (_trackChecksumActionStart != null)
			{
				instance.ActionExecutor.BeforeActionExecuted -= _trackChecksumActionStart;
			}
			if (_generateMissingPostActionChecksum != null)
			{
				instance.ActionExecutor.AfterActionExecuted -= _generateMissingPostActionChecksum;
			}
			_trackChecksumActionStart = null;
			_generateMissingPostActionChecksum = null;
			_checksumTrackingFallbackInstalled = false;
		}
	}

	private void RemoveActionExecutionTracking()
	{
		if (_actionExecutionTrackingInstalled)
		{
			if (_trackExecutedAction != null)
			{
				RunManager.Instance.ActionExecutor.AfterActionExecuted -= _trackExecutedAction;
			}
			_trackExecutedAction = null;
			_actionExecutionTrackingInstalled = false;
		}
	}

	private static async Task ResetEnvironmentAsync(NGame game)
	{
		RemoveBackendIncompatibleCombatStateSubscribers();
		if (RunManager.Instance.DebugOnlyGetState() != null)
		{
			try
			{
				RunManager.Instance.CleanUp(true);
			}
			catch (Exception value)
			{
				Log.Warn($"[{CombatTestBootstrap.LogPrefix}] Cleanup raised an exception: {value}", 2);
			}
		}
		game.RootSceneContainer.SetCurrentScene(new Control
		{
			Name = "CombatTestBlankScene"
		});
		await WaitForStatic(() => NRun.Instance == null && RunManager.Instance.DebugOnlyGetState() == null && CombatManager.Instance.DebugOnlyGetState() == null && !CombatManager.Instance.IsInProgress, "Previous combat environment did not reset cleanly.");
		await NextFrame();
		await NextFrame();
	}

	private static void RemoveBackendIncompatibleCombatStateSubscribers()
	{
		if (TestMode.IsOn && !(CombatStateChangedField == null))
		{
			CombatStateTracker stateTracker = CombatManager.Instance.StateTracker;
			if (CombatStateChangedField.GetValue(stateTracker) is Delegate obj)
			{
				CombatStateChangedField.SetValue(stateTracker, null);
				Log.Warn($"[{CombatTestBootstrap.LogPrefix}] Removed {obj.GetInvocationList().Length} " + "CombatStateChanged subscriber(s) that are invalid while STS2 TestMode is active.", 2);
			}
		}
	}

	private static async Task WaitForStatic(Func<bool> condition, string timeoutMessage)
	{
		DateTime deadline = DateTime.UtcNow + WaitTimeout;
		while (!condition())
		{
			if (DateTime.UtcNow >= deadline)
			{
				throw new InvalidOperationException(timeoutMessage);
			}
			await NextFrame();
		}
	}

	private static async Task NextFrame()
	{
		SceneTree val = (SceneTree)Engine.GetMainLoop();
		await ((GodotObject)val).ToSignal((GodotObject)val, "process_frame");
	}
}
