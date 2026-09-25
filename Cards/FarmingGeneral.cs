#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Enchantments;
using Squ.Audio;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

/// <summary>种地将军：从抽牌堆选择至多两（三）张可播种或带消耗的牌，并分别处理其可用效果。</summary>
[RegisterCard(typeof(TokenCardPool), StableEntryStem = "farming_general")]
public sealed class FarmingGeneral : ModCardTemplate
{
	public const int BaseMaxCards = 2;
	public const int UpgradedMaxCards = 3;

	private static readonly LocString SelectionPrompt =
		new("cards", "SUNQIAN_UNIVERSE_CARD_FARMING_GENERAL.selectionScreenPrompt");

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..HoverTipFactory.FromEnchantment<Sown>(),
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
	];

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(BaseMaxCards),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/FarmingGeneral.png");

	public FarmingGeneral()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.FarmingGeneralEvent);
		IEnumerable<CardModel> selectedCards = await CardSelectCmd.FromCombatPile(
			choiceContext,
			PileType.Draw.GetPile(Owner),
			Owner,
			new CardSelectorPrefs(SelectionPrompt, 0, DynamicVars.Cards.IntValue),
			IsEligible);

		foreach (CardModel selectedCard in selectedCards)
		{
			if (ModelDb.Enchantment<Sown>().CanEnchant(selectedCard))
			{
				CardCmd.Enchant<Sown>(selectedCard, 1m);
			}

			if (selectedCard.Keywords.Contains(CardKeyword.Exhaust))
			{
				CardCmd.RemoveKeyword(selectedCard, CardKeyword.Exhaust);
			}
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(UpgradedMaxCards - BaseMaxCards);
	}

	private static bool IsEligible(CardModel card) =>
		card.Keywords.Contains(CardKeyword.Exhaust)
		|| ModelDb.Enchantment<Sown>().CanEnchant(card);
}
