using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
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
		ChickenFootCheeseStrikePower.ShouldRedirectBasicStrike(card);

	public static async Task ExecuteRedirectedBasicStrikeDamage(
		CardModel card,
		PlayerChoiceContext choiceContext)
	{
		foreach (Creature target in GetBasicStrikeRedirectTargets(card))
		{
			if (!target.IsAlive)
			{
				continue;
			}

			await DamageCmd.Attack(card.DynamicVars.Damage.BaseValue)
				.FromCard(card)
				.Targeting(target)
				.WithHitFx("vfx/vfx_attack_slash")
				.Execute(choiceContext);
		}
	}

	public static List<Creature> GetBasicStrikeRedirectTargets(CardModel card) =>
		SquRandomEnemyTargeting.GetTargets(card, null);
}
