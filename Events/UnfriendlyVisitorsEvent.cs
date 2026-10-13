using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using Squ.Audio;
using Squ.Cards;
using Squ.Interop;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Events;

[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Underdocks))]
public sealed class UnfriendlyVisitors : ModEventTemplate
{
	private const string ResultPage = "RESULT";
	private const string CaoWeiInitialPage = "CAO_WEI_INITIAL";
	private const string CaoWeiTauntResultPage = "CAO_WEI_TAUNT_RESULT";
	private const string CaoWeiShrugItOffResultPage = "CAO_WEI_SHRUG_IT_OFF_RESULT";
	private const string CaoWeiSuckerPunchResultPage = "CAO_WEI_SUCKER_PUNCH_RESULT";
	private const string ShuHanInitialPage = "SHU_HAN_INITIAL";
	private const string ShuHanWatchFireResultPage = "SHU_HAN_WATCH_FIRE_RESULT";
	private const string ShuHanProxyStrikeResultPage = "SHU_HAN_PROXY_STRIKE_RESULT";
	private const string ShuHanGoForTheEyesResultPage = "SHU_HAN_GO_FOR_THE_EYES_RESULT";
	private const string NewsanguoProxyStrikeEntry = "NEWSANGUO_CARD_PROXY_STRIKE";

	public override EventAssetProfile AssetProfile => new(
		InitialPortraitPath: "res://images/events/UnfriendlyVisitorsEvent.png");

	public override LocString InitialDescription => UsesShuHanText
		? PageDescription(ShuHanInitialPage)
		: UsesCaoWeiText
			? PageDescription(CaoWeiInitialPage)
			: base.InitialDescription;

	public override bool IsAllowed(IRunState runState) =>
		runState.Players.All(player => player.Deck.Cards.Any(IsBasicStrikeOrDefend));

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		CardModel? defend = Owner!.Deck.Cards.FirstOrDefault(card => IsBasicCardWithTag(card, CardTag.Defend));
		CardModel? strike = Owner.Deck.Cards.FirstOrDefault(card => IsBasicCardWithTag(card, CardTag.Strike));
		IHoverTip defendTip = defend is null
			? HoverTipFactory.FromCard<DefendIronclad>()
			: HoverTipFactory.FromCard(defend);
		IHoverTip strikeTip = strike is null
			? HoverTipFactory.FromCard<StrikeIronclad>()
			: HoverTipFactory.FromCard(strike);

		if (UsesShuHanText)
		{
			return GenerateShuHanOptions(defend, strike, defendTip, strikeTip);
		}

		bool usesCaoWeiText = UsesCaoWeiText;
		string initialPage = usesCaoWeiText ? CaoWeiInitialPage : "INITIAL";

		return
		[
			new EventOption(
				this,
				defend is not null
					? () => TransformBasicCard<Taunt>(
						CardTag.Defend,
						usesCaoWeiText ? CaoWeiTauntResultPage : ResultPage,
						SquSfx.UnfriendlyVisitorsTauntEvent)
					: null,
				ModOptionKey(initialPage, "TAUNT"),
				[defendTip, .. HoverTipFactory.FromCardWithCardHoverTips<Taunt>()]),
			new EventOption(
				this,
				defend is not null
					? () => TransformBasicCard<ShrugItOff>(
						CardTag.Defend,
						usesCaoWeiText ? CaoWeiShrugItOffResultPage : ResultPage,
						SquSfx.UnfriendlyVisitorsShrugItOffEvent)
					: null,
				ModOptionKey(initialPage, "SHRUG_IT_OFF"),
				[defendTip, .. HoverTipFactory.FromCardWithCardHoverTips<ShrugItOff>()]),
			new EventOption(
				this,
				strike is not null
					? () => TransformBasicCard<SuckerPunch>(
						CardTag.Strike,
						usesCaoWeiText ? CaoWeiSuckerPunchResultPage : ResultPage,
						SquSfx.UnfriendlyVisitorsSuckerPunchEvent)
					: null,
				ModOptionKey(initialPage, "SUCKER_PUNCH"),
				[strikeTip, .. HoverTipFactory.FromCardWithCardHoverTips<SuckerPunch>()]),
		];
	}

	private IReadOnlyList<EventOption> GenerateShuHanOptions(
		CardModel? defend,
		CardModel? strike,
		IHoverTip defendTip,
		IHoverTip strikeTip)
	{
		CardModel? proxyStrike = ModelDb.GetByIdOrNull<CardModel>(
			new ModelId("CARD", NewsanguoProxyStrikeEntry));
		IReadOnlyList<IHoverTip> proxyStrikeHoverTips = proxyStrike is null
			? [strikeTip]
			: [strikeTip, HoverTipFactory.FromCard(proxyStrike)];

		return
		[
			new EventOption(
				this,
				defend is not null
					? () => TransformBasicCard<WatchFireFromShore>(
						CardTag.Defend,
						ShuHanWatchFireResultPage,
						SquSfx.UnfriendlyVisitorsTauntEvent)
					: null,
				ModOptionKey(ShuHanInitialPage, "WATCH_FIRE_FROM_SHORE"),
				[defendTip, .. HoverTipFactory.FromCardWithCardHoverTips<WatchFireFromShore>()]),
			new EventOption(
				this,
				strike is not null && proxyStrike is not null
					? () => TransformBasicCard(
						CardTag.Strike,
						proxyStrike,
						ShuHanProxyStrikeResultPage,
						SquSfx.UnfriendlyVisitorsShrugItOffEvent)
					: null,
				ModOptionKey(ShuHanInitialPage, "PROXY_STRIKE"),
				proxyStrikeHoverTips),
			new EventOption(
				this,
				strike is not null
					? () => TransformBasicCard<GoForTheEyes>(
						CardTag.Strike,
						ShuHanGoForTheEyesResultPage,
						SquSfx.UnfriendlyVisitorsSuckerPunchEvent)
					: null,
				ModOptionKey(ShuHanInitialPage, "GO_FOR_THE_EYES"),
				[strikeTip, .. HoverTipFactory.FromCardWithCardHoverTips<GoForTheEyes>()]),
		];
	}

	private bool UsesCaoWeiText =>
		NewsanguoPublicApiInterop.IsReady
		&& NewsanguoPublicApiInterop.IsCaoWeiCharacter(Owner?.Character);

	private bool UsesShuHanText =>
		NewsanguoPublicApiInterop.IsReady
		&& NewsanguoPublicApiInterop.IsShuHanCharacter(Owner?.Character);

	private Task TransformBasicCard<T>(CardTag tag, string resultPage, string sfxEvent)
		where T : CardModel
		=> TransformBasicCard(
			tag,
			selectedCard => selectedCard.Owner.RunState.CreateCard<T>(selectedCard.Owner),
			resultPage,
			sfxEvent);

	private Task TransformBasicCard(
		CardTag tag,
		CardModel replacementCanonical,
		string resultPage,
		string sfxEvent)
		=> TransformBasicCard(
			tag,
			selectedCard => selectedCard.Owner.RunState.CreateCard(replacementCanonical, selectedCard.Owner),
			resultPage,
			sfxEvent);

	private async Task TransformBasicCard(
		CardTag tag,
		Func<CardModel, CardModel> createReplacement,
		string resultPage,
		string sfxEvent)
	{
		SquSfx.Play(sfxEvent);

		CardModel? selectedCard = (await CardSelectCmd.FromDeckGeneric(
			Owner!,
			new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1),
			card => IsBasicCardWithTag(card, tag))).FirstOrDefault();

		if (selectedCard is not null)
		{
			CardModel replacement = createReplacement(selectedCard);
			if (selectedCard.IsUpgraded)
			{
				CardCmd.Upgrade(replacement, CardPreviewStyle.None);
			}

			await CardCmd.Transform(selectedCard, replacement, CardPreviewStyle.EventLayout);
		}

		SetEventFinished(PageDescription(resultPage));
	}

	private static bool IsBasicStrikeOrDefend(CardModel card) =>
		card.Rarity == CardRarity.Basic
		&& (card.Tags.Contains(CardTag.Strike) || card.Tags.Contains(CardTag.Defend));

	private static bool IsBasicCardWithTag(CardModel card, CardTag tag) =>
		card.Rarity == CardRarity.Basic
		&& card.Tags.Contains(tag);
}
