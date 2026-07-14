using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// 由模型（遗物、能力、剧本等）实现，用于调整即将发生的一次[gold]预见[/gold]的数量。
/// 监听器会在预见结算前，按 <see cref="MegaCrit.Sts2.Core.Combat.ICombatState.IterateHookListeners"/>
/// 的顺序被 <see cref="ScryHook.ModifyScryAmount"/> 依次调用。
/// </summary>
public interface IModifyScryAmount
{
	/// <summary>
	/// 返回调整后的预见数量。每次待处理的预见调用一次，收到的是经过更早监听器修改后的值。
	/// 返回原值即视为不参与（也会被排除出 <see cref="AfterModifyingScryAmount"/> 回调）。
	/// 结果不做下限约束：降到 0 或以下会直接取消本次预见。此方法应对游戏状态保持纯净，
	/// 副作用（特效、音效、消耗充能）请放到 <see cref="AfterModifyingScryAmount"/> 中。
	/// </summary>
	int ModifyScryAmount(Player player, int amount);

	/// <summary>
	/// 所有监听器执行完毕后调用，但仅对确实改变了所收到值的监听器触发。
	/// 用于实现「修改了预见」这一行为的副作用（视觉、音效、扣减计数等）。
	/// 在任何卡牌被查看前运行，且即便最终数量与原值相等或非正也会运行。
	/// </summary>
	Task AfterModifyingScryAmount(PlayerChoiceContext ctx, Player player, int originalAmount, int modifiedAmount);
}
