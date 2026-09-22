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

#nullable enable

namespace Squ.Combat;

/// <summary>Combat-only targets and shared UI/interaction behavior for Slight Revision.</summary>
public static class SlightRevisionSystem
{
	private readonly record struct Revision(CardModel Target, bool TargetUpgraded);

	private static readonly Dictionary<CardModel, Revision> GrantedRevisions = [];
	private static IDisposable? _rightClickBinding;
	private static bool _initialized;

	public static void Initialize()
	{
		if (_initialized) return;
		_initialized = true;
		RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(_ => GrantedRevisions.Clear());
		RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(_ => GrantedRevisions.Clear());
		_rightClickBinding = ModRightClickRegistry.Register<CardModel>(
			SquMod.ModId, "granted_slight_revision", ExecuteGrantedRevision, priority: 1,
			canHandleLocal: context => CanExecute(context.Player, context.Model as CardModel),
			canExecute: context => CanExecute(context.Player, context.Model as CardModel));
	}

	public static bool GrantUltimateDefend(CardModel card, bool upgraded) =>
		Grant(card, ModelDb.Card<UltimateDefend>(), upgraded);

	public static bool Grant(CardModel card, CardModel target, bool targetUpgraded)
	{
		if (card.Keywords.Contains(SquKeywords.SlightRevision)) return false;
		GrantedRevisions[card] = new Revision(target, targetUpgraded);
		CardCmd.ApplyKeyword(card, SquKeywords.SlightRevision);
		return true;
	}

	public static void AddDescription(LocString description, CardModel target, bool targetUpgraded) =>
		AddDescription(description, new Revision(target, targetUpgraded));

	public static void AddGrantedDescription(LocString description, CardModel card)
	{
		if (GrantedRevisions.TryGetValue(card, out Revision revision)) AddDescription(description, revision);
		else description.Add("SlightRevisionText", string.Empty);
	}

	public static string GetGrantedDescriptionSuffix(CardModel card) =>
		GrantedRevisions.TryGetValue(card, out Revision revision)
			? GetDescriptionText(revision)
			: string.Empty;

	public static IEnumerable<IHoverTip> GetHoverTips(CardModel target, bool targetUpgraded) =>
	[
		HoverTipFactory.FromCard(target, targetUpgraded),
		..target.HoverTips,
	];

	public static IEnumerable<IHoverTip> GetGrantedHoverTips(CardModel card) =>
		GrantedRevisions.TryGetValue(card, out Revision revision)
			? GetHoverTips(revision.Target, revision.TargetUpgraded)
			: [];

	public static Task TransformAsync(
		CardModel original, CardModel target, bool targetUpgraded) =>
		TransformAsync(original, new Revision(target, targetUpgraded));

	private static void AddDescription(LocString description, Revision revision)
	{
		LocString text = new("card_keywords", "SUNQIAN_UNIVERSE_KEYWORD_SLIGHT_REVISION.cardDescription");
		text.Add("Title", ModKeywordRegistry.GetTitle(SquKeywords.SlightRevisionId));
		text.Add("TargetCardName", GetDisplayTitle(revision));
		SquKeywords.AddNestedLoc(description, "SlightRevisionText", text);
	}

	private static string GetDescriptionText(Revision revision)
	{
		LocString text = new("card_keywords", "SUNQIAN_UNIVERSE_KEYWORD_SLIGHT_REVISION.cardDescription");
		text.Add("Title", ModKeywordRegistry.GetTitle(SquKeywords.SlightRevisionId));
		text.Add("TargetCardName", GetDisplayTitle(revision));
		return text.GetFormattedText();
	}

	private static string GetDisplayTitle(Revision revision)
	{
		CardModel display = revision.Target.ToMutable();
		if (revision.TargetUpgraded)
		{
			display.UpgradeInternal();
			display.FinalizeUpgradeInternal();
		}
		return display.Title;
	}

	private static bool CanExecute(Player player, CardModel? card) =>
		card is not null && card.Owner == player && card.Pile?.Type == PileType.Hand
		&& card.IsTransformable && GrantedRevisions.ContainsKey(card);

	private static async Task ExecuteGrantedRevision(ModRightClickExecutionContext context)
	{
		if (context.Model is not CardModel original || !CanExecute(context.Player, original)
			|| !GrantedRevisions.Remove(original, out Revision revision)) return;
		await TransformAsync(original, revision);
	}

	private static async Task TransformAsync(CardModel original, Revision revision)
	{
		if (original.CardScope is not { } scope) return;
		CardModel replacement = scope.CreateCard(revision.Target, original.Owner);
		if (revision.TargetUpgraded)
		{
			replacement.UpgradeInternal();
			replacement.FinalizeUpgradeInternal();
		}
		await CardCmd.Transform(original, replacement);
	}
}
