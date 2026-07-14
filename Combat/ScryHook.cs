using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// [gold]预见[/gold]的 hook 分发器。通过 <see cref="ICombatState.IterateHookListeners"/> 枚举当前战斗中的
/// 所有 hook 监听模型（能力、遗物、药水、牌堆里的卡等），把值传递给实现了预见接口的监听器。
/// 因此任何能力/遗物/卡牌只要实现 <see cref="IModifyScryAmount"/> 或 <see cref="IAfterScryed"/> 即可参与，
/// 无需额外订阅。
/// </summary>
public static class ScryHook
{
	/// <summary>
	/// 让所有 <see cref="IModifyScryAmount"/> 监听器依次调整预见数量，并输出实际改变了该值的监听器集合。
	/// </summary>
	public static int ModifyScryAmount(Player player, int amount, out IEnumerable<IModifyScryAmount> modifiers)
	{
		return Modify(player.Creature.CombatState, amount,
			(m, current) => m.ModifyScryAmount(player, current), out modifiers);
	}

	/// <summary>
	/// 在所有修改器执行完毕后，仅对之前确实改变了值的监听器触发 <see cref="IModifyScryAmount.AfterModifyingScryAmount"/>。
	/// </summary>
	public static Task AfterModifyingScryAmount(PlayerChoiceContext ctx, Player player,
		IEnumerable<IModifyScryAmount> modifiers, int originalAmount, int modifiedAmount)
	{
		var combatState = player.Creature.CombatState;
		if (combatState == null) return Task.CompletedTask;
		return AfterModifying(combatState, modifiers,
			m => m.AfterModifyingScryAmount(ctx, player, originalAmount, modifiedAmount));
	}

	/// <summary>
	/// 预见完全结算后，向所有 <see cref="IAfterScryed"/> 监听器分发。
	/// </summary>
	public static Task AfterScryed(PlayerChoiceContext ctx, Player player, int scryAmount, int discardedAmount,
		IEnumerable<CardModel> discarded)
	{
		return Dispatch<IAfterScryed>(ctx, player,
			m => m.AfterScryed(ctx, player, scryAmount, discardedAmount, discarded));
	}

	private static int Modify<THook>(ICombatState? combatState, int originalAmount,
		Func<THook, int, int> amountModifier, out IEnumerable<THook> modifiers)
		where THook : class
	{
		if (combatState == null)
		{
			modifiers = [];
			return originalAmount;
		}

		var amount = originalAmount;
		var changed = new List<THook>();
		foreach (var model in combatState.IterateHookListeners().OfType<THook>())
		{
			var previous = amount;
			amount = amountModifier(model, amount);
			if (previous != amount)
				changed.Add(model);
		}

		modifiers = changed;
		return amount;
	}

	private static async Task AfterModifying<THook>(ICombatState combatState, IEnumerable<THook> modifiers,
		Func<THook, Task> action)
		where THook : class
	{
		var modifierSet = new HashSet<THook>(modifiers);
		foreach (var listener in combatState.IterateHookListeners().OfType<THook>())
		{
			if (!modifierSet.Contains(listener)) continue;
			await action(listener);
			if (listener is AbstractModel model)
				model.InvokeExecutionFinished();
		}
	}

	private static async Task Dispatch<T>(PlayerChoiceContext ctx, Player player, Func<T, Task> invoke)
		where T : class
	{
		var combatState = player.Creature.CombatState;
		if (combatState == null) return;
		foreach (var model in combatState.IterateHookListeners().OfType<T>())
		{
			var abstractModel = (AbstractModel)(object)model;
			ctx.PushModel(abstractModel);
			await invoke(model);
			ctx.PopModel(abstractModel);
		}
	}
}
