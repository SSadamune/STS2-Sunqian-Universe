using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "lens_reshoot")]
public sealed class LensReshoot : ModCardTemplate
{
	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.Script),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/LensReshoot.png");

	public LensReshoot()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		List<CardModel> scriptCards = PileType.Discard.GetPile(Owner).Cards
			.Where(IsScriptCard)
			.ToList();
		if (scriptCards.Count == 0)
		{
			return;
		}

		CardModel? scriptCard = Owner.RunState.Rng.CombatCardGeneration.NextItem(scriptCards);
		if (scriptCard is null || scriptCard.Pile?.Type != PileType.Discard)
		{
			return;
		}

		await CardPileCmd.Add(scriptCard, PileType.Hand);
		scriptCard.EnergyCost.SetThisTurn(0);
	}

	protected override void OnUpgrade()
	{
		RemoveKeyword(CardKeyword.Exhaust);
		AddKeyword(CardKeyword.Retain);
	}

	private static bool IsScriptCard(CardModel card) =>
		card.Tags.Contains(SquCardTags.Script);
}
