using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using Squ.Cards;
using Squ.Powers;

#nullable enable

namespace Squ.Combat;

/// <summary>博望悖论剧本激活时的强制二选一。</summary>
public static class BowangParadoxChoice
{
	private static readonly LocString SelectionScreenPrompt =
		new("cards", "SUNQIAN_UNIVERSE_CARD_BOWANG_PARADOX_SCRIPT.selectionScreenPrompt");

	public static async Task OfferAsync(
		PlayerChoiceContext choiceContext,
		Player player,
		CardModel? source,
		bool upgraded,
		decimal? reinforcedAmount = null)
	{
		if (player.Creature.CombatState is not { } combatState)
		{
			return;
		}

		CardModel vigorChoice = combatState.CreateCard<BowangParadoxVigorChoice>(player);
		CardModel tinderChoice = combatState.CreateCard<BowangParadoxTinderChoice>(player);
		if (upgraded)
		{
			UpgradeAndFinalize(vigorChoice);
			UpgradeAndFinalize(tinderChoice);
		}
		decimal effectAmount = reinforcedAmount ?? (upgraded
			? BowangParadoxScript.UpgradedVigor
			: BowangParadoxScript.BaseVigor);
		vigorChoice.DynamicVars[nameof(VigorPower)].BaseValue = effectAmount;
		tinderChoice.DynamicVars[nameof(TinderPower)].BaseValue = effectAmount;

		CardModel? chosen = await CardSelectCmd.FromChooseACardScreen(
			choiceContext,
			[vigorChoice, tinderChoice],
			player,
			canSkip: false);
		if (chosen is BowangParadoxVigorChoice)
		{
			await PowerCmd.Apply<VigorPower>(
				choiceContext,
				player.Creature,
				effectAmount,
				player.Creature,
				source);
			await DiscardMatchingCards(
				choiceContext,
				player,
				card => card.Type == CardType.Attack && !card.ShouldRetainThisTurn);
		}
		else if (chosen is BowangParadoxTinderChoice)
		{
			await PowerCmd.Apply<TinderPower>(
				choiceContext,
				player.Creature,
				effectAmount,
				player.Creature,
				source);
			await DiscardMatchingCards(
				choiceContext,
				player,
				card => SquCardTags.AppliesBurning(card) && !card.ShouldRetainThisTurn);
		}
	}

	private static void UpgradeAndFinalize(CardModel card)
	{
		card.UpgradeInternal();
		card.FinalizeUpgradeInternal();
	}

	private static async Task DiscardMatchingCards(
		PlayerChoiceContext choiceContext,
		Player player,
		System.Func<CardModel, bool> predicate)
	{
		List<CardModel> cards = PileType.Hand.GetPile(player).Cards
			.Where(predicate)
			.ToList();
		if (cards.Count > 0)
		{
			await CardCmd.DiscardAndDraw(choiceContext, cards, 0);
		}
	}

	[HarmonyPatch(typeof(NChooseACardSelectionScreen), nameof(NChooseACardSelectionScreen._Ready))]
	private static class SelectionScreenTitlePatch
	{
		private static void Postfix(NChooseACardSelectionScreen __instance)
		{
			if (AccessTools.Field(typeof(NChooseACardSelectionScreen), "_cards")
					.GetValue(__instance) is not IReadOnlyList<CardModel> cards
				|| cards.Count != 2
				|| !cards.Any(card => card is BowangParadoxVigorChoice)
				|| !cards.Any(card => card is BowangParadoxTinderChoice)
				|| AccessTools.Field(typeof(NChooseACardSelectionScreen), "_banner")
					.GetValue(__instance) is not NCommonBanner banner)
			{
				return;
			}

			banner.label.SetTextAutoSize(SelectionScreenPrompt.GetRawText());
		}
	}
}
