#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using Squ.Powers;
using STS2RitsuLib;

namespace Squ.Combat;

/// <summary>Future contributions can use AddContribution; the balance lasts one combat.</summary>
public static class LifeContractSystem
{
	private sealed class Account { public int Total; public bool Claimed; }
	private static readonly ConditionalWeakTable<ICombatState, Dictionary<(Player, string), Account>> Battles = new();
	public static void Initialize() => RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(evt =>
	{
		if (evt.CombatState != null) Battles.Remove(evt.CombatState);
	});
	private static Account? GetAccount(Player player, string companionId)
	{
		if (player.Creature.CombatState is not { } combat) return null;
		var accounts = Battles.GetOrCreateValue(combat);
		if (!accounts.TryGetValue((player, companionId), out Account? account)) accounts.Add((player, companionId), account = new());
		return account;
	}
	public static int GetTotal(Player player, string companionId = CompanionSummons.SecondBrotherId) => GetAccount(player, companionId)?.Total ?? 0;
	public static void AddContribution(Player player, int actualHpLost, string companionId = CompanionSummons.SecondBrotherId)
	{
		if (actualHpLost <= 0 || GetAccount(player, companionId) is not { Claimed: false } account) return;
		account.Total = (int)Math.Min(999999999L, (long)account.Total + actualHpLost);
		CompanionSummons.GetCompanion(player, companionId)
			?.GetPower<LifeContractPower>()?.Refresh();
	}
	internal static int Claim(Player player, string companionId = CompanionSummons.SecondBrotherId)
	{
		if (GetAccount(player, companionId) is not { Claimed: false } account) return 0;
		account.Claimed = true;
		return account.Total;
	}
}
