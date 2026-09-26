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

internal interface ISlightRevisionSource
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
				|| !CanExecute(context.Player, card)
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
				context.Player.NetId);
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
		if (card.Capability<SlightRevisionCapability>() is not null) return false;
		card.GetOrCreateCapability<SlightRevisionCapability>().Configure(target, targetUpgraded);
		CardCmd.ApplyKeyword(card, SquKeywords.SlightRevision);
		return true;
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

	private static bool CanExecute(Player player, CardModel? card) =>
		card is not null && card.Owner == player && card.Pile?.Type == PileType.Hand
		&& card.IsTransformable;

	private static bool TryGetRevision(CardModel card, out CardModel target, out bool targetUpgraded)
	{
		if (card is ISlightRevisionSource source)
		{
			target = source.SlightRevisionTarget;
			targetUpgraded = source.SlightRevisionTargetUpgraded;
			return true;
		}

		if (card.Capability<SlightRevisionCapability>() is { } revision)
		{
			target = revision.Target;
			targetUpgraded = revision.TargetUpgraded;
			return true;
		}

		target = null!;
		targetUpgraded = false;
		return false;
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
			|| !CanExecute(context.Player, original))
		{
			return;
		}

		CardModel target = ModelDb.GetById<CardModel>(ModelId.Deserialize(payload.TargetId));
		await TransformAsync(original, target, payload.TargetUpgraded);
	}

}
