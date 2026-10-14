using System;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Quality;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Platform;

namespace TestTheSpire;

internal sealed class TestNetGameService : INetGameService
{
	private readonly ulong _netId;

	private readonly NetGameType _type;

	private bool _isGameLoading;

	public ulong NetId => _netId;

	public bool IsConnected => true;

	public bool IsGameLoading => _isGameLoading;

	public NetGameType Type
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return _type;
		}
	}

	public PlatformType Platform => (PlatformType)0;

	public event Action<NetErrorInfo>? Disconnected;

	public TestNetGameService(ulong netId, NetGameType type = (NetGameType)2)
	{
		_netId = netId;
		_type = type;
	}

	public void SendMessage<T>(T message, ulong playerId) where T : INetMessage
	{
	}

	public void SendMessage<T>(T message) where T : INetMessage
	{
	}

	public void RegisterMessageHandler<T>(MessageHandlerDelegate<T> messageHandlerDelegate) where T : INetMessage
	{
	}

	public void UnregisterMessageHandler<T>(MessageHandlerDelegate<T> messageHandlerDelegate) where T : INetMessage
	{
	}

	public void Update()
	{
	}

	public void Disconnect(NetError reason, bool now = false)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		Disconnected?.Invoke(new NetErrorInfo(reason, true));
	}

	public ConnectionStats? GetStatsForPeer(ulong peerId)
	{
		return null;
	}

	public void SetGameLoading(bool isLoading)
	{
		_isGameLoading = isLoading;
	}

	public void SetBufferMessages(bool bufferMessages)
	{
	}

	public string? GetRawLobbyIdentifier()
	{
		return null;
	}
}
