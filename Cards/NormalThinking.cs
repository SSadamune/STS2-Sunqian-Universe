using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Squ.Interop;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 常人思维：固有、虚无且不能打出。启用新三国时，被虚无会失去 3 点天意之力。
/// </summary>
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "normal_thinking")]
public sealed class NormalThinking : ModCardTemplate
{
	public const int HeavensForceLoss = 3;

	public override int MaxUpgradeLevel => 0;

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Innate,
		CardKeyword.Ethereal,
		CardKeyword.Unplayable,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips
	{
		get
		{
			if (NewsanguoPublicApiInterop.IsReady)
			{
				yield return NewsanguoPublicApiInterop.CreateHeavensForceHoverTip();
			}
		}
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/NormalThinking.png");

	public NormalThinking()
		: base(-1, CardType.Curse, CardRarity.Curse, TargetType.None, showInCardLibrary: false)
	{
	}

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
		Task.CompletedTask;

	public override Task AfterCardExhausted(
		PlayerChoiceContext choiceContext,
		CardModel card,
		bool causedByEthereal)
	{
		if (card != this || !causedByEthereal || !NewsanguoPublicApiInterop.IsReady)
		{
			return Task.CompletedTask;
		}

		return NewsanguoPublicApiInterop.AddHeavensForce(
			choiceContext,
			Owner,
			-HeavensForceLoss,
			this);
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		description.Add(
			"NewsanguoEffect",
			NewsanguoPublicApiInterop.IsReady
				? new LocString("cards", Id.Entry + ".newsanguoEffect").GetFormattedText()
				: string.Empty);
	}
}
