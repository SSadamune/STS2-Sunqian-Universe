using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Events;

[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Underdocks))]
public sealed class UnfriendlyVisitors : ModEventTemplate
{
	private const string ResultPage = "RESULT";

	public override EventAssetProfile AssetProfile => new(
		InitialPortraitPath: "res://images/events/UnfriendlyVisitorsEvent.png");

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

		return
		[
			new EventOption(
				this,
				defend is not null ? () => TransformBasicCard<Taunt>(CardTag.Defend) : null,
				InitialOptionKey("TAUNT"),
				[defendTip, .. HoverTipFactory.FromCardWithCardHoverTips<Taunt>()]),
			new EventOption(
				this,
				defend is not null ? () => TransformBasicCard<ShrugItOff>(CardTag.Defend) : null,
				InitialOptionKey("SHRUG_IT_OFF"),
				[defendTip, .. HoverTipFactory.FromCardWithCardHoverTips<ShrugItOff>()]),
			new EventOption(
				this,
				strike is not null ? () => TransformBasicCard<SuckerPunch>(CardTag.Strike) : null,
				InitialOptionKey("SUCKER_PUNCH"),
				[strikeTip, .. HoverTipFactory.FromCardWithCardHoverTips<SuckerPunch>()]),
		];
	}

	private async Task TransformBasicCard<T>(CardTag tag)
		where T : CardModel
	{
		CardModel? selectedCard = (await CardSelectCmd.FromDeckGeneric(
			Owner!,
			new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1),
			card => IsBasicCardWithTag(card, tag))).FirstOrDefault();

		if (selectedCard is not null)
		{
			CardModel replacement = selectedCard.Owner.RunState.CreateCard<T>(selectedCard.Owner);
			if (selectedCard.IsUpgraded)
			{
				CardCmd.Upgrade(replacement, CardPreviewStyle.None);
			}

			await CardCmd.Transform(selectedCard, replacement, CardPreviewStyle.EventLayout);
		}

		SetEventFinished(PageDescription(ResultPage));
	}

	private static bool IsBasicStrikeOrDefend(CardModel card) =>
		card.Rarity == CardRarity.Basic
		&& (card.Tags.Contains(CardTag.Strike) || card.Tags.Contains(CardTag.Defend));

	private static bool IsBasicCardWithTag(CardModel card, CardTag tag) =>
		card.Rarity == CardRarity.Basic
		&& card.Tags.Contains(tag);
}
