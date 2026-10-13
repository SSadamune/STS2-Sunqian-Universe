using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Networking.ManagedActions;
using Squ.Audio;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// 可由硬引用本模组的外部卡牌实现，使其作为固有的「稍作修改」来源参与统一右键逻辑。
/// 使用可选依赖的模组应改用公开 API 附加 <see cref="SlightRevisionCapability"/>。
/// </summary>
public interface ISlightRevisionSource
{
	CardModel SlightRevisionTarget { get; }

	bool SlightRevisionTargetUpgraded { get; }
}

/// <summary>Shared interaction and intrinsic-card presentation behavior for Slight Revision.</summary>
public static class SlightRevisionSystem
{
	private readonly record struct SlightRevisionPayload(
		uint CombatCardIndex,
		string OriginalId,
		string TargetId,
		bool TargetUpgraded);

	private sealed class SlightRevisionRightClickHandler : IModRightClickHandler
	{
		// Consume Slight Revision before RitsuLib's generic model-identity handler.
		// NetCombatCard is the same stable locator used by vanilla multiplayer card actions.
		public int Priority => 100;

		public bool TryHandle(ModRightClickContext context)
		{
			if (context.Model is not CardModel card
				|| !RequestRevision(context.Player, card))
			{
				return false;
			}

			return true;
		}
	}

	private static readonly RitsuLibManagedNetActionDescriptor<SlightRevisionPayload> SlightRevisionAction = new(
		SquMod.ModId,
		"slight_revision",
		static payload => JsonSerializer.SerializeToUtf8Bytes(payload),
		static bytes => JsonSerializer.Deserialize<SlightRevisionPayload>(bytes),
		ExecuteSyncedRevision,
		GameActionType.CombatPlayPhaseOnly);

	private static readonly SlightRevisionRightClickHandler RightClickHandler = new();
	private static bool _initialized;

	public static void Initialize()
	{
		if (_initialized) return;
		_initialized = true;
		RitsuLibManagedNetActions.Register(SlightRevisionAction);
		ModRightClickRegistry.Register(RightClickHandler);
	}

	public static bool GrantUltimateDefend(CardModel card, bool upgraded) =>
		Grant(card, ModelDb.Card<UltimateDefend>(), upgraded);

	public static bool Grant(CardModel card, CardModel target, bool targetUpgraded)
	{
		Set(card, target, targetUpgraded);
		return true;
	}

	/// <summary>
	/// 为可变卡牌附加或更新「稍作修改」。Capability 会随卡牌克隆、存档及联机状态同步。
	/// </summary>
	public static void Set(CardModel card, CardModel target, bool targetUpgraded)
	{
		ArgumentNullException.ThrowIfNull(card);
		ArgumentNullException.ThrowIfNull(target);

		card.GetOrCreateCapability<SlightRevisionCapability>().Configure(target, targetUpgraded);
		if (!card.Keywords.Contains(SquKeywords.SlightRevision))
		{
			CardCmd.ApplyKeyword(card, SquKeywords.SlightRevision);
		}
	}

	public static void AddDescription(LocString description, CardModel target, bool targetUpgraded)
	{
		LocString text = new("card_keywords", "SUNQIAN_UNIVERSE_KEYWORD_SLIGHT_REVISION.cardDescription");
		text.Add("Title", ModKeywordRegistry.GetTitle(SquKeywords.SlightRevisionId));
		text.Add("TargetCardName", GetDisplayTitle(target, targetUpgraded));
		SquKeywords.AddNestedLoc(description, "SlightRevisionText", text);
	}

	public static IEnumerable<IHoverTip> GetHoverTips(CardModel target, bool targetUpgraded) =>
	[
		HoverTipFactory.FromCard(target, targetUpgraded),
		..target.HoverTips,
	];

	public static async Task TransformAsync(CardModel original, CardModel target, bool targetUpgraded)
	{
		if (original.CardScope is not { } scope) return;
		SquSfx.Play(SquSfx.SlightRevisionEvent);
		CardModel replacement = scope.CreateCard(target, original.Owner);
		if (targetUpgraded)
		{
			replacement.UpgradeInternal();
			replacement.FinalizeUpgradeInternal();
		}
		await CardCmd.Transform(original, replacement);
	}

	public static string GetDisplayTitle(CardModel target, bool targetUpgraded)
	{
		CardModel display = target.ToMutable();
		if (targetUpgraded)
		{
			display.UpgradeInternal();
			display.FinalizeUpgradeInternal();
		}
		return display.Title;
	}

	public static bool CanExecute(Player player, CardModel? card) =>
		card is not null && card.Owner == player && card.Pile?.Type == PileType.Hand
		&& card.IsTransformable && TryGetRevision(card, out _, out _);

	public static bool TryGetRevision(CardModel card, out CardModel target, out bool targetUpgraded)
	{
		// A granted capability is the single mutable Slight Revision slot. It must win over
		// an intrinsic source so granting Slight Revision again replaces the old target.
		if (card.Capability<SlightRevisionCapability>() is { } revision)
		{
			target = revision.Target;
			targetUpgraded = revision.TargetUpgraded;
			return true;
		}

		if (card.Keywords.Contains(SquKeywords.SlightRevision)
			&& card is ISlightRevisionSource source)
		{
			target = source.SlightRevisionTarget;
			targetUpgraded = source.SlightRevisionTargetUpgraded;
			return true;
		}

		target = null!;
		targetUpgraded = false;
		return false;
	}

	/// <summary>
	/// 请求执行卡牌的「稍作修改」。成功请求会进入 RitsuLib 联机动作队列，并在所有端同步结算。
	/// </summary>
	public static bool RequestRevision(Player player, CardModel card)
	{
		if (!CanExecute(player, card)
			|| !TryGetRevision(card, out CardModel target, out bool targetUpgraded))
		{
			return false;
		}

		NetCombatCard netCard = NetCombatCard.FromModel(card);
		SlightRevisionPayload payload = new(
			netCard.CombatCardIndex,
			card.Id.ToString(),
			target.Id.ToString(),
			targetUpgraded);

		return RitsuLibManagedNetActions.Request(
			RunManager.Instance,
			SlightRevisionAction,
			payload,
			player.NetId);
	}

	private static async Task ExecuteSyncedRevision(
		RitsuLibManagedNetActionContext<SlightRevisionPayload> context)
	{
		SlightRevisionPayload payload = context.Message;
		// Resolve independently on every peer only when the queued action executes.
		CardModel? original = NetCombatCard
			.ForTesting(payload.CombatCardIndex)
			.ToCardModelOrNull();
		if (original is null
			|| original.Id != ModelId.Deserialize(payload.OriginalId)
			|| !CanExecute(context.Player, original)
			|| !TryGetRevision(original, out CardModel target, out bool targetUpgraded)
			|| target.Id != ModelId.Deserialize(payload.TargetId)
			|| targetUpgraded != payload.TargetUpgraded)
		{
			return;
		}

		await TransformAsync(original, target, targetUpgraded);
	}

}
