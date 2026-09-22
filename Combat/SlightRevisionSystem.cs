using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models.Capabilities;
using Squ.Audio;

#nullable enable

namespace Squ.Combat;

/// <summary>Shared interaction and intrinsic-card presentation behavior for Slight Revision.</summary>
public static class SlightRevisionSystem
{
	private static IDisposable? _rightClickBinding;
	private static bool _initialized;

	public static void Initialize()
	{
		if (_initialized) return;
		_initialized = true;
		_rightClickBinding = ModRightClickRegistry.Register<CardModel>(
			SquMod.ModId, "granted_slight_revision", ExecuteGrantedRevision, priority: 1,
			canHandleLocal: context => CanExecute(context.Player, context.Model as CardModel),
			canExecute: context => CanExecute(context.Player, context.Model as CardModel));
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
		&& card.IsTransformable && card.Capability<SlightRevisionCapability>() is not null;

	private static async Task ExecuteGrantedRevision(ModRightClickExecutionContext context)
	{
		if (context.Model is not CardModel original || !CanExecute(context.Player, original)) return;
		SlightRevisionCapability? revision = original.Capability<SlightRevisionCapability>();
		if (revision is null) return;
		await TransformAsync(original, revision.Target, revision.TargetUpgraded);
	}

}
