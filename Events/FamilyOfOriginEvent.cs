using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using Squ.Audio;
using Squ.Cards;
using Squ.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Events;

[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Underdocks))]
[RegisterActEvent(typeof(Hive))]
public sealed class FamilyOfOrigin : ModEventTemplate
{
	// 正文是上一章的阅读结果，本页选项才是下一章。
	// 爱宠 → 蛐蛐罐；至亲 → 神奇生物；伴侣 → 正妻斟酒。
	private const string CutePetPage = "CUTE_PET";
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
		SquSfx.Play(SquSfx.NeverHadTheseCutePetOptionEvent);
		await AddCardToDeck<CutePet>();
		await RelicCmd.Obtain<NeverHadTheseRelic>(Owner!);
		SetEventState(PageDescription(CutePetPage),
		[
			new EventOption(
				this,
				ReadNextOfKin,
				ModOptionKey(CutePetPage, "READ_NEXT_OF_KIN"),
				ReadOptionHoverTips<NextOfKin>()),
			new EventOption(this, Reject, ModOptionKey(CutePetPage, "REJECT")),
		]);
	}

	private async Task ReadNextOfKin()
	{
		SquSfx.Play(SquSfx.NeverHadTheseNextOfKinOptionEvent);
		await AddCardToDeck<NextOfKin>();
		SetEventState(PageDescription(NextOfKinPage),
		[
			new EventOption(
				this,
				ReadBelovedPartner,
				ModOptionKey(NextOfKinPage, "READ_BELOVED_PARTNER"),
				ReadOptionHoverTips<BelovedPartner>()),
			new EventOption(this, Reject, ModOptionKey(NextOfKinPage, "REJECT")),
		]);
	}

	private async Task ReadBelovedPartner()
	{
		SquSfx.Play(SquSfx.NeverHadTheseBelovedPartnerOptionEvent);
		await AddCardToDeck<BelovedPartner>();
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
