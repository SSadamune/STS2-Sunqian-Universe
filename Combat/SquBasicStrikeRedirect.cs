using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Squ.Powers;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Redirected basic-strike targeting and damage for chicken-foot-cheese.
/// </summary>
public static class SquBasicStrikeRedirect
{
	public static bool ShouldHandleInOnPlay(CardModel card) =>
		ChickenFootCheeseStrikePower.ShouldRedirectBasicStrike(card);

	public static async Task ExecuteRedirectedBasicStrikeDamage(
		CardModel card,
		PlayerChoiceContext choiceContext,
		CardPlay? cardPlay = null)
	{
		if (card.Owner?.Creature?.GetPower<ChickenFootCheeseStrikePower>() is { } power)
		{
			power.TriggerForRedirectedStrike();
		}

		await SquRandomEnemyTargeting.ExecuteDistinctRandomEnemyDamage(
			card,
			choiceContext,
			ChickenFootCheeseStrikePower.RedirectRandomEnemyCount,
			hitCountPerTarget: ChickenFootCheeseStrikePower.RedirectHitCountPerTarget,
			cardPlay: cardPlay);
	}
}
