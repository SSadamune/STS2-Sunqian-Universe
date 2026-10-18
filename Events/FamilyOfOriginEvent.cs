using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using Squ.Cards;
using Squ.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Events;

[RegisterActEvent(typeof(Hive))]
public sealed class FamilyOfOrigin : ModEventTemplate
{
	private const string PartnerPage = "PARTNER";
	private const string NextOfKinPage = "NEXT_OF_KIN";
	private const string CompletedPage = "COMPLETED";
	private const string RejectedPage = "REJECTED";
	private const string FirstRejectedPage = "FIRST_REJECTED";

	public override EventAssetProfile AssetProfile => new(
		InitialPortraitPath: "res://images/events/NeverHadThese.png");

	protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
	[
		new EventOption(
			this,
			ReadCutePet,
			InitialOptionKey("READ_CUTE_PET"),
			ReadOptionHoverTips<CutePet>()),
		new EventOption(
			this,
			RejectAtFirstStage,
			InitialOptionKey("REJECT"),
			HoverTipFactory.FromCardWithCardHoverTips<NormalThinking>()),
	];

	private async Task ReadCutePet()
	{
		await AddCardToDeck<CutePet>();
		await RelicCmd.Obtain<NeverHadTheseRelic>(Owner!);
		SetEventState(PageDescription(PartnerPage),
		[
			new EventOption(
				this,
				ReadBelovedPartner,
				ModOptionKey(PartnerPage, "READ_BELOVED_PARTNER"),
				ReadOptionHoverTips<BelovedPartner>()),
			new EventOption(this, Reject, ModOptionKey(PartnerPage, "REJECT")),
		]);
	}

	private async Task ReadBelovedPartner()
	{
		await AddCardToDeck<BelovedPartner>();
		SetEventState(PageDescription(NextOfKinPage),
		[
			new EventOption(
				this,
				ReadNextOfKin,
				ModOptionKey(NextOfKinPage, "READ_NEXT_OF_KIN"),
				ReadOptionHoverTips<NextOfKin>()),
			new EventOption(this, Reject, ModOptionKey(NextOfKinPage, "REJECT")),
		]);
	}

	private async Task ReadNextOfKin()
	{
		await AddCardToDeck<NextOfKin>();
		SetEventFinished(PageDescription(CompletedPage));
	}

	private async Task RejectAtFirstStage()
	{
		await AddCardToDeck<NormalThinking>();
		SetEventFinished(PageDescription(FirstRejectedPage));
	}

	private Task Reject()
	{
		SetEventFinished(PageDescription(RejectedPage));
		return Task.CompletedTask;
	}

	private async Task AddCardToDeck<T>()
		where T : CardModel
	{
		CardModel card = Owner!.RunState.CreateCard<T>(Owner);
		await CardPileCmd.Add(card, PileType.Deck);
	}

	private static IEnumerable<IHoverTip> ReadOptionHoverTips<T>()
		where T : CardModel
	{
		foreach (IHoverTip tip in HoverTipFactory.FromCardWithCardHoverTips<T>())
		{
			yield return tip;
		}

		// 只悬停遗物本身。能量和敏捷的说明仍留在遗物自己的悬停里。
		yield return ModelDb.Relic<NeverHadTheseRelic>().HoverTip;
	}
}
