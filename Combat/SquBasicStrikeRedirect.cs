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
		ScriptChickenFootCheesePower.ShouldRedirectBasicStrike(card);

	public static async Task ExecuteRedirectedBasicStrikeDamage(
		CardModel card,
		PlayerChoiceContext choiceContext,
		CardPlay? cardPlay = null)
	{
		ScriptChickenFootCheesePower? power =
			card.Owner?.Creature?.GetPower<ScriptChickenFootCheesePower>();
		power?.TriggerForRedirectedStrike();

		await SquRandomEnemyTargeting.ExecuteDistinctRandomEnemyDamage(
			card,
			choiceContext,
			ScriptChickenFootCheesePower.RedirectRandomEnemyCount,
			hitCountPerTarget: power?.HitCountPerTarget ?? ScriptChickenFootCheesePower.BaseHitCount,
			cardPlay: cardPlay);
	}
}
