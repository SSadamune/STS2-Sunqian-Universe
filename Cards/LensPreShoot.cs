using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Squ.Audio;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 镜头预拍：从抽牌堆随机获得一张剧本牌；升级后改为由玩家选择。
/// </summary>
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "lens_pre_shoot")]
public sealed class LensPreShoot : ModCardTemplate
{
	private static readonly LocString SelectionPrompt =
		new("cards", "SUNQIAN_UNIVERSE_CARD_LENS_PRE_SHOOT.selectionScreenPrompt");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.Script),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/LensReshoot.png");

	protected override bool IsPlayable =>
		PileType.Draw.GetPile(Owner).Cards.Any(IsScriptCard);

	public LensPreShoot()
		: base(1, CardType.Skill, CardRarity.Token, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		CardPile drawPile = PileType.Draw.GetPile(Owner);
		List<CardModel> scriptCards = drawPile.Cards.Where(IsScriptCard).ToList();
		if (scriptCards.Count == 0)
		{
			return;
		}

		CardModel? scriptCard;
		if (IsUpgraded)
		{
			IEnumerable<CardModel> selected = await CardSelectCmd.FromCombatPile(
				choiceContext,
				drawPile,
				Owner,
				new CardSelectorPrefs(SelectionPrompt, 1),
				IsScriptCard);
			scriptCard = selected.FirstOrDefault();
		}
		else
		{
			scriptCard = Owner.RunState.Rng.CombatCardGeneration.NextItem(scriptCards);
		}

		if (scriptCard?.Pile?.Type != PileType.Draw)
		{
			return;
		}

		SquSfx.Play(IsUpgraded
			? SquSfx.LensReshootGoodSpiritEvent
			: SquSfx.LensReshootShowHeWasThereEvent);
		await CardPileCmd.Add(scriptCard, PileType.Hand);
		scriptCard.EnergyCost.SetThisTurn(0);
	}

	private static bool IsScriptCard(CardModel card) =>
		card.Tags.Contains(SquCardTags.Script);
}
