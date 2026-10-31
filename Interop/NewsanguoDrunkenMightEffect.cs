using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using Squ.Powers;

#nullable enable

namespace Squ.Interop;

/// <summary>
/// 资源效果联动：模组已加载时，指定牌的「消耗活力 / 受到活力加成」同时计入酒力。
/// 该模式依据角色当前是否拥有酒力生效，不要求角色来自新三国模组；它不替换获得
/// 活力的效果，也不走 <see cref="NewsanguoVigorSwap"/> 的角色转换分支。
/// </summary>
internal static class NewsanguoDrunkenMightEffect
{
	public const string PowerEntry = "NEWSANGUO_POWER_DRUNKEN_MIGHT_POWER";

	internal enum Presentation
	{
		None,
		Hover,
		Inline,
	}

	public static bool IsEnabled => NewsanguoPublicApiInterop.IsReady;

	public static bool IsDrunkenMight(PowerModel power) =>
		IsEnabled && power.Id.Entry == PowerEntry;

	/// <summary>资源效果模式：该角色当前是否能让酒力参与联动效果。</summary>
	public static bool CanUseResourceEffects(Creature? creature) =>
		GetAmount(creature) > 0;

	/// <summary>资源效果模式下可被指定效果追踪的活力或酒力。</summary>
	public static bool IsEffectResource(PowerModel power, Creature owner) =>
		power.Owner == owner && (power is VigorPower || IsDrunkenMight(power));

	public static int GetAmount(Creature? creature) =>
		IsEnabled ? NewsanguoPublicApiInterop.GetDrunkenMight(creature?.Player) : 0;

	/// <summary>
	/// 打出攻击牌后酒力减半（向下取整）所消耗的层数。减半发生在出牌结束后，
	/// 卡牌结算中要按这个差额预支。
	/// </summary>
	public static int ConsumedByAttack(Creature creature)
	{
		int amount = GetAmount(creature);
		return amount - amount / 2;
	}

	/// <summary>
	/// 主动执行一次攻击牌应有的酒力减半，并保留明确的卡牌来源。用于本身不创建
	/// AttackCommand、但需要把这次资源加成继承给子攻击的上将军根牌。
	/// </summary>
	public static async Task<int> SpendForAttack(
		PlayerChoiceContext choiceContext,
		Creature creature,
		CardModel cardSource)
	{
		int consumed = ConsumedByAttack(creature);
		if (consumed <= 0)
		{
			return 0;
		}

		PowerModel? drunkenMight = null;
		foreach (PowerModel power in creature.Powers)
		{
			if (IsDrunkenMight(power))
			{
				drunkenMight = power;
				break;
			}
		}

		if (drunkenMight is null)
		{
			return 0;
		}

		await PowerCmd.ModifyAmount(
			choiceContext,
			drunkenMight,
			-consumed,
			creature,
			cardSource);
		return consumed;
	}

	/// <summary>
	/// 酒力已经会加到攻击上的总倍数：本体算 1，吾亦过江再加 1，活力增幅按这张牌的倍率再加。
	/// </summary>
	public static int GetAttackScale(Creature creature, CardModel? card)
	{
		int scale = creature.GetPower<CrossTheRiverPower>() is { Amount: > 0 } ? 2 : 1;
		if (card is not null
			&& creature.GetPower<VigorAmplificationPower>() is { } amplification)
		{
			scale += amplification.GetBonusMultiplierFor(card);
		}

		return scale;
	}

	/// <summary>
	/// 不走攻击伤害钩子的效果（例如灼烧）要自己加上的酒力，含吾亦过江与活力增幅。
	/// </summary>
	public static int GetBonusForCardPreview(CardModel card) =>
		card.Pile?.Type is PileType.Hand or PileType.Play && card.Owner?.Creature is { } owner
			? GetBonus(owner, card)
			: 0;

	public static int GetBonus(Creature creature, CardModel? card = null)
	{
		int amount = GetAmount(creature);
		if (amount <= 0)
		{
			return 0;
		}

		return amount * GetAttackScale(creature, card);
	}

	public static Presentation ResolvePresentation()
	{
		RunState? runState = RunManager.Instance.DebugOnlyGetState();
		bool inCombat = runState is not null && CombatManager.Instance is { IsInProgress: true };
		bool localIsNewsanguo = false;
		bool localHasDrunkenMight = false;
		if (inCombat && runState is not null)
		{
			var localPlayer = LocalContext.GetMe(runState);
			localIsNewsanguo = NewsanguoPublicApiInterop.IsNewsanguoCharacter(localPlayer?.Character);
			localHasDrunkenMight = CanUseResourceEffects(localPlayer?.Creature);
		}

		return Resolve(IsEnabled, inCombat, localIsNewsanguo, localHasDrunkenMight);
	}

	internal static Presentation Resolve(
		bool isModReady,
		bool isInCombat,
		bool localPlayerIsNewsanguoCharacter,
		bool localPlayerHasDrunkenMight)
	{
		if (!isModReady)
		{
			return Presentation.None;
		}

		if (!isInCombat)
		{
			return Presentation.Hover;
		}

		return localPlayerIsNewsanguoCharacter || localPlayerHasDrunkenMight
			? Presentation.Inline
			: Presentation.None;
	}

	public static void AddOrText(LocString description) =>
		description.Add("DrunkenMightOr", OrText());

	public static string OrText() =>
		ResolvePresentation() == Presentation.Inline
			? SquCommonL10n.NewsanguoDrunkenMightOrText().GetFormattedText()
			: string.Empty;

	public static IEnumerable<IHoverTip> HoverTips()
	{
		if (ResolvePresentation() != Presentation.Hover)
		{
			yield break;
		}

		yield return new HoverTip(
			SquCommonL10n.AnnotationTitle(),
			SquCommonL10n.NewsanguoDrunkenMightEffectHover());
	}
}
