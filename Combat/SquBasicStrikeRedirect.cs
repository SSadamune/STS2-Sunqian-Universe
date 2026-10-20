using System.Threading.Tasks;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
		if (card.Owner?.Creature is not { } dealer)
		{
			return;
		}
		power?.TriggerForRedirectedStrike();

		List<Creature> targets = SquRandomEnemyTargeting.SelectRandomEnemies(
			card,
			ScriptChickenFootCheesePower.RedirectRandomEnemyCount,
			cardPlay?.Target);
		if (targets.Count == 0)
		{
			return;
		}

		SquVigorSnapshot.AttackSequence damageSequence =
			SquVigorSnapshot.BeginAttackSequence(dealer, card);
		await SquRandomEnemyTargeting.ExecuteDamageAgainstTargets(
			card,
			choiceContext,
			targets,
			damageSequence.ResolveNextAttackDamage(),
			hitFx: null,
			cardPlay: cardPlay,
			beforeHit: SquHitVfx.SmallMagicMissiles);
		await SquRandomEnemyTargeting.ExecuteDamageAgainstTargets(
			card,
			choiceContext,
			targets,
			damageSequence.ResolveNextAttackDamage(
				power?.FollowUpDamage ?? ScriptChickenFootCheesePower.BaseFollowUpDamage),
			hitFx: null,
			cardPlay: cardPlay,
			beforeHit: SquHitVfx.SmallMagicMissiles);
	}
}
