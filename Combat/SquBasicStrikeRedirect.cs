using System.Threading.Tasks;
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

	public static Task ExecuteRedirectedBasicStrikeDamage(
		CardModel card,
		PlayerChoiceContext choiceContext) =>
		SquRandomEnemyTargeting.ExecuteDistinctRandomEnemyDamage(
			card,
			choiceContext,
			ChickenFootCheeseStrikePower.RedirectRandomEnemyCount);
}
