using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "audition")]
public sealed class Audition : ModCardTemplate
{
	private const int ScriptCardCount = 3;

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.Script),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/Audition.png");

	public Audition()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		List<CardModel> scriptCards = BuildDistinctScriptCards(ScriptCardCount);
		if (IsUpgraded)
		{
			foreach (CardModel card in scriptCards)
			{
				card.UpgradeInternal();
				card.FinalizeUpgradeInternal();
			}
		}

		foreach (CardModel card in scriptCards)
		{
			await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
		}
	}

	private List<CardModel> BuildDistinctScriptCards(int count) =>
		CardFactory.GetDistinctForCombat(
			Owner,
			Owner.Character.CardPool.GetUnlockedCards(
				Owner.UnlockState,
				Owner.RunState.CardMultiplayerConstraint)
				.Where(card => card.Tags.Contains(SquCardTags.Script)),
			count,
			Owner.RunState.Rng.CombatCardGeneration).ToList();
}
