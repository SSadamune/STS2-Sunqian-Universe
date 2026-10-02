using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ.Character;
using Squ.Powers;
using Squ.Script;

#nullable enable

namespace Squ.Api;

/// <summary>
/// 孙乾宇宙提供给其他模组的稳定、可选依赖 API。
/// 调用方应通过 RitsuLib 的 ModInterop 或 AssemblyInterop 访问本类型，
/// 不需要直接引用 sunqian-universe.dll。
/// </summary>
public static class SunqianUniversePublicApi
{
	/// <summary>API 已加载且可以调用。</summary>
	public static bool IsReady => true;

	/// <summary>判断角色是否为本模组的「龙套演员」。</summary>
	public static bool IsSupportingActorCharacter(CharacterModel? character) =>
		character is SunqianCharacter;

	/// <summary>当前进行中的战斗里是否至少有一名「龙套演员」玩家。</summary>
	public static bool HasSupportingActorInCombat()
	{
		CombatManager manager = CombatManager.Instance;
		return manager.IsInProgress
			&& manager.DebugOnlyGetState()?.Players.Any(player =>
				IsSupportingActorCharacter(player.Character)) == true;
	}

	/// <summary>判断能力是否为本模组的「灼烧」。</summary>
	public static bool IsBurningPower(PowerModel? power) => power is BurningPower;

	/// <summary>创建「灼烧」的能力悬停说明。</summary>
	public static IHoverTip CreateBurningHoverTip() =>
		HoverTipFactory.FromPower<BurningPower>();

	/// <summary>
	/// 使用本模组的完整灼烧结算逻辑施加「灼烧」，包括其他模组对能力施加流程的钩子。
	/// </summary>
	public static async Task<PowerModel?> ApplyBurning(
		PlayerChoiceContext choiceContext,
		Creature target,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		bool silent = false) =>
		await PowerCmd.Apply<BurningPower>(
			choiceContext,
			target,
			amount,
			applier,
			cardSource,
			silent);

	/// <summary>判断能力是否属于本模组的「剧本」机制。</summary>
	public static bool IsScriptPower(PowerModel? power) => power is ScriptPowerTemplate;

	/// <summary>取得生物当前的剧本；没有剧本时返回 null。</summary>
	public static PowerModel? GetActiveScript(Creature? creature) =>
		creature?.Powers.OfType<ScriptPowerTemplate>().FirstOrDefault();

	/// <summary>判断生物当前是否有剧本。</summary>
	public static bool HasActiveScript(Creature? creature) =>
		creature is not null && ScriptSystem.HasActiveScript(creature);

	/// <summary>
	/// 施加一个由本模组注册的剧本能力。传入规范实例或可变实例均可；API 会创建独立实例，
	/// 并执行新剧本替换旧剧本、旧剧本失效通知等完整生命周期。
	/// </summary>
	/// <exception cref="ArgumentException">传入的能力不属于本模组的剧本机制。</exception>
	public static async Task ApplyScriptPower(
		PlayerChoiceContext choiceContext,
		PowerModel scriptPower,
		Creature target,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		bool silent = false)
	{
		ArgumentNullException.ThrowIfNull(scriptPower);
		ArgumentNullException.ThrowIfNull(target);

		if (scriptPower is not ScriptPowerTemplate)
		{
			throw new ArgumentException(
				"The supplied power is not a Sunqian Universe script power.",
				nameof(scriptPower));
		}

		PowerModel instance = scriptPower.IsCanonical
			? scriptPower.ToMutable()
			: (PowerModel)scriptPower.MutableClone();

		await PowerCmd.Apply(
			choiceContext,
			instance,
			target,
			amount,
			applier,
			cardSource,
			silent);
	}

	/// <summary>令生物身上的所有剧本失效，并正常触发剧本失效效果。</summary>
	public static Task InvalidateScripts(Creature creature) =>
		ScriptSystem.InvalidateScriptsAsync(creature);

	/// <summary>
	/// 移除指定剧本。notifyLift 为 true 时正常触发剧本失效效果；为 false 时静默移除。
	/// </summary>
	/// <exception cref="ArgumentException">传入的能力不属于本模组的剧本机制。</exception>
	public static Task RemoveScript(PowerModel power, bool notifyLift = true)
	{
		ArgumentNullException.ThrowIfNull(power);

		if (power is not ScriptPowerTemplate scriptPower)
		{
			throw new ArgumentException(
				"The supplied power is not a Sunqian Universe script power.",
				nameof(power));
		}

		return ScriptSystem.RemoveScriptPowerAsync(scriptPower, notifyLift);
	}

	/// <summary>取得该玩家本回合已触发的剧本失效次数。</summary>
	public static int GetScriptLiftsThisTurn(Player? player) =>
		player is null ? 0 : ScriptSystem.GetScriptLiftsThisTurn(player);
}
