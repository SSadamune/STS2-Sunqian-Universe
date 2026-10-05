using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "lens_reshoot")]
public sealed class LensReshoot : ModCardTemplate, ISlightRevisionSource
{
	CardModel ISlightRevisionSource.SlightRevisionTarget => ModelDb.Card<LensPreShoot>();

	bool ISlightRevisionSource.SlightRevisionTargetUpgraded => false;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips
	{
		get
		{
			yield return HoverTipFactory.FromKeyword(SquKeywords.Script);
			if (IsUpgraded && this.Capability<SlightRevisionCapability>() is null)
			{
				foreach (IHoverTip tip in SlightRevisionSystem.GetHoverTips(
					ModelDb.Card<LensPreShoot>(),
					targetUpgraded: false))
				{
					yield return tip;
				}
			}
		}
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/LensReshoot.png");

	protected override bool IsPlayable =>
		PileType.Discard.GetPile(Owner).Cards.Any(IsScriptCard);

	public LensReshoot()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (IsUpgraded)
		{
			SquSfx.Play(SquSfx.LensReshootGoodSpiritEvent);
		}
		else
		{
			SquSfx.PlayRandom(
				RunState,
				SquSfx.LensReshootShowHeWasThereEvent,
				SquSfx.LensReshootZhaoYunDidntComeEvent);
		}

		List<CardModel> scriptCards = PileType.Discard.GetPile(Owner).Cards
			.Where(IsScriptCard)
			.ToList();
		if (scriptCards.Count == 0)
		{
			return;
		}

		CardModel? scriptCard = Owner.RunState.Rng.CombatCardGeneration.NextItem(scriptCards);
		if (scriptCard?.Pile?.Type is not (PileType.Discard or PileType.Draw))
		{
			return;
		}

		await CardPileCmd.Add(scriptCard, PileType.Hand);
		scriptCard.EnergyCost.SetThisTurn(0);
	}

	protected override void OnUpgrade()
	{
		AddKeyword(SquKeywords.SlightRevision);
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		if (this.Capability<SlightRevisionCapability>() is null)
		{
			SlightRevisionSystem.AddDescription(
				description,
				ModelDb.Card<LensPreShoot>(),
				targetUpgraded: false);
			return;
		}

		description.Add("SlightRevisionText", "");
	}

	private static bool IsScriptCard(CardModel card) =>
		card.Tags.Contains(SquCardTags.Script);
}
