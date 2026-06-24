using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Squ.Cards;

#nullable enable

namespace Squ.Script;

internal static class EatSomethingCardLogic
{
	public static async Task DrawAndExhaustFromHandAsync(
		PlayerChoiceContext choiceContext,
		Player player,
		CardModel source,
		int drawCount,
		int exhaustCount)
	{
		if (drawCount > 0)
		{
			await CardPileCmd.Draw(choiceContext, drawCount, player);
		}

		if (exhaustCount <= 0)
		{
			return;
		}

		CardSelectorPrefs prefs = new(CardSelectorPrefs.ExhaustSelectionPrompt, exhaustCount, exhaustCount);
		IEnumerable<CardModel> toExhaust = await CardSelectCmd.FromHand(
			choiceContext,
			player,
			prefs,
			null,
			source);

		foreach (CardModel card in toExhaust)
		{
			await CardCmd.Exhaust(choiceContext, card);
		}
	}

	public static async Task GrantExactlyWhatToEatToOtherPlayersAsync(
		Player owner,
		CardModel source,
		bool upgradedToken)
	{
		if (owner.RunState.Players.Count <= 1)
		{
			return;
		}

		ICombatState? combatState = owner.Creature.CombatState;
		if (combatState is null)
		{
			return;
		}

		foreach (Player otherPlayer in combatState.Players.Where(player => player != owner))
		{
			await GeneratedCombatCards.AddToHandInCombat<ExactlyWhatToEat>(
				combatState,
				otherPlayer,
				upgradedToken,
				owner);
		}
	}
}
