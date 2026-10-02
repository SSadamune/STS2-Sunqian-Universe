using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Squ.Character;
using Squ.Combat;
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

	/// <summary>「稍作修改」关键词的稳定限定 ID。</summary>
	public static string SlightRevisionKeywordId => SquKeywords.SlightRevisionId;

	/// <summary>取得「稍作修改」关键词。</summary>
	public static CardKeyword SlightRevisionKeyword => SquKeywords.SlightRevision;

	/// <summary>创建「稍作修改」关键词的悬停说明。</summary>
	public static IHoverTip CreateSlightRevisionHoverTip() =>
		HoverTipFactory.FromKeyword(SquKeywords.SlightRevision);

	/// <summary>
	/// 为可变卡牌赋予「稍作修改」。卡牌已有固有或动态配置时，新的目标会覆盖旧目标；
	/// 关键词和 capability 始终只保留一份。
	/// </summary>
	public static bool GrantSlightRevision(
		CardModel card,
		CardModel target,
		bool targetUpgraded = false) =>
		SlightRevisionSystem.Grant(card, target, targetUpgraded);

	/// <summary>
	/// 为可变卡牌附加或更新「稍作修改」。此方法会添加关键词和可持久化 capability；
	/// 之后右键该手牌会自动使用本模组的联机同步逻辑变为目标牌。
	/// </summary>
	public static void SetSlightRevision(
		CardModel card,
		CardModel target,
		bool targetUpgraded = false) =>
		SlightRevisionSystem.Set(card, target, targetUpgraded);

	/// <summary>判断卡牌是否具有可执行的「稍作修改」配置。</summary>
	public static bool HasSlightRevision(CardModel? card) =>
		card is not null && SlightRevisionSystem.TryGetRevision(card, out _, out _);

	/// <summary>取得「稍作修改」的目标牌规范实例；没有配置时返回 null。</summary>
	public static CardModel? GetSlightRevisionTarget(CardModel? card) =>
		card is not null
		&& SlightRevisionSystem.TryGetRevision(card, out CardModel target, out _)
			? target
			: null;

	/// <summary>目标牌是否会以升级状态生成；没有配置时返回 false。</summary>
	public static bool IsSlightRevisionTargetUpgraded(CardModel? card) =>
		card is not null
		&& SlightRevisionSystem.TryGetRevision(card, out _, out bool targetUpgraded)
		&& targetUpgraded;

	/// <summary>当前玩家现在是否可以对指定手牌执行「稍作修改」。</summary>
	public static bool CanExecuteSlightRevision(Player player, CardModel? card) =>
		SlightRevisionSystem.CanExecute(player, card);

	/// <summary>
	/// 请求执行「稍作修改」。返回是否成功进入联机动作队列；通常无需手动调用，
	/// 因为本模组的全局右键处理器会自动处理已配置的卡牌。
	/// </summary>
	public static bool RequestSlightRevision(Player player, CardModel card) =>
		SlightRevisionSystem.RequestRevision(player, card);

	/// <summary>把固有「稍作修改」的说明文本参数添加到卡牌描述。</summary>
	public static void AddSlightRevisionDescription(
		LocString description,
		CardModel target,
		bool targetUpgraded = false) =>
		SlightRevisionSystem.AddDescription(description, target, targetUpgraded);

	/// <summary>取得固有「稍作修改」应展示的目标牌悬停说明。</summary>
	public static IEnumerable<IHoverTip> GetSlightRevisionHoverTips(
		CardModel target,
		bool targetUpgraded = false) =>
		SlightRevisionSystem.GetHoverTips(target, targetUpgraded);
}
