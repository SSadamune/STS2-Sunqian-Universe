using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace TestTheSpire;

internal sealed record CombatTestBattleDefinition(IReadOnlyList<CombatTestBattleBuilder.PlayerFactory> Players, ulong LocalNetId, bool UseMultiplayerHost, Func<EncounterModel> EncounterFactory, RoomType RoomType, bool RemoveStartingRelics, string? Seed)
{
	public CombatTestBattleDefinition WithLocalNetId(ulong localNetId)
	{
		if (Players.All((CombatTestBattleBuilder.PlayerFactory player) => player.NetId != localNetId))
		{
			throw new InvalidOperationException($"Local net ID {localNetId} was not found in the configured combat test players.");
		}
		return this with
		{
			LocalNetId = localNetId,
			UseMultiplayerHost = (UseMultiplayerHost || Players.Count > 1)
		};
	}

	[CompilerGenerated]
	public void Deconstruct(out IReadOnlyList<CombatTestBattleBuilder.PlayerFactory> Players, out ulong LocalNetId, out bool UseMultiplayerHost, out Func<EncounterModel> EncounterFactory, out RoomType RoomType, out bool RemoveStartingRelics, out string? Seed)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected I4, but got Unknown
		Players = this.Players;
		LocalNetId = this.LocalNetId;
		UseMultiplayerHost = this.UseMultiplayerHost;
		EncounterFactory = this.EncounterFactory;
		RoomType = (RoomType)(int)this.RoomType;
		RemoveStartingRelics = this.RemoveStartingRelics;
		Seed = this.Seed;
	}
}
