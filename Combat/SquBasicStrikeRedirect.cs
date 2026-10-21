using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Powers;
using STS2RitsuLib.Combat.CardTargeting;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// Redirected basic-strike targeting and damage for chicken-foot-cheese.
/// </summary>
public static class SquBasicStrikeRedirect
{
	private static readonly System.Reflection.PropertyInfo? CardPlayTargetProperty =
		typeof(CardPlay).GetProperty(nameof(CardPlay.Target));

	internal sealed class RedirectScope
	{
		public required CardModel Card { get; init; }

		public required CardPlay CardPlay { get; init; }

		public required Creature? OriginalTarget { get; init; }

		public required List<Creature> Targets { get; init; }

		public required int FollowUpDamage { get; init; }

		public required int VigorSnapshot { get; init; }

		public bool SawPoweredAttack { get; set; }

		public bool IsAppendingFollowUp { get; set; }
	}

	private static readonly Dictionary<CardModel, List<RedirectScope>> ActiveScopes = [];

	public static bool ShouldHandleInOnPlay(CardModel card) =>
		ScriptChickenFootCheesePower.ShouldRedirectBasicStrike(card);

	internal static RedirectScope? BeginRedirectedBasicStrike(
		CardModel card,
		CardPlay cardPlay)
	{
		ScriptChickenFootCheesePower? power =
			card.Owner?.Creature?.GetPower<ScriptChickenFootCheesePower>();
		if (card.Owner?.Creature is not { } dealer)
		{
			return null;
		}

		List<Creature> targets = SquRandomEnemyTargeting.SelectRandomEnemies(
			card,
			ScriptChickenFootCheesePower.RedirectRandomEnemyCount,
			cardPlay.Target);
		if (targets.Count == 0)
		{
			return null;
		}

		power?.TriggerForRedirectedStrike();
		var scope = new RedirectScope
		{
			Card = card,
			CardPlay = cardPlay,
			OriginalTarget = cardPlay.Target,
			Targets = targets,
			FollowUpDamage = power?.FollowUpDamage
				?? ScriptChickenFootCheesePower.BaseFollowUpDamage,
			VigorSnapshot = SquVigorSnapshot.GetEffectiveAmount(dealer, card),
		};
		SetCardPlayTarget(cardPlay, targets[0]);
		if (!ActiveScopes.TryGetValue(card, out List<RedirectScope>? scopes))
		{
			scopes = [];
			ActiveScopes.Add(card, scopes);
		}

		scopes.Add(scope);
		return scope;
	}

	internal static void TryRedirectAttack(AttackCommand command)
	{
		if (command.ModelSource is not CardModel card
			|| !ActiveScopes.TryGetValue(card, out List<RedirectScope>? scopes))
		{
			return;
		}

		RedirectScope? scope = scopes.LastOrDefault(candidate =>
			!candidate.IsAppendingFollowUp
			&& (ReferenceEquals(candidate.CardPlay, command.CardPlay)
				|| command.CardPlay is null));
		if (scope is null)
		{
			return;
		}

		command.TargetingFiltered(scope.Targets);
		if (command.DamageProps.IsPoweredAttack())
		{
			scope.SawPoweredAttack = true;
		}
	}

	internal static async Task CompleteRedirectedBasicStrike(
		Task originalPlay,
		RedirectScope scope,
		PlayerChoiceContext choiceContext)
	{
		try
		{
			await originalPlay;
			scope.IsAppendingFollowUp = true;
			if (scope.Card.Owner?.Creature is not { } dealer)
			{
				return;
			}

			int currentVigor = SquVigorSnapshot.GetEffectiveAmount(dealer, scope.Card);
			decimal carriedVigor = scope.SawPoweredAttack
				? System.Math.Max(0, scope.VigorSnapshot - currentVigor)
				: 0m;
			await SquRandomEnemyTargeting.ExecuteDamageAgainstTargets(
				scope.Card,
				choiceContext,
				scope.Targets,
				scope.FollowUpDamage + carriedVigor,
				hitFx: null,
				cardPlay: scope.CardPlay,
				beforeHit: SquHitVfx.SmallMagicMissiles);
		}
		finally
		{
			EndScope(scope);
		}
	}

	internal static void AbortRedirectedBasicStrike(RedirectScope scope) =>
		EndScope(scope);

	private static void EndScope(RedirectScope scope)
	{
		SetCardPlayTarget(scope.CardPlay, scope.OriginalTarget);
		if (!ActiveScopes.TryGetValue(scope.Card, out List<RedirectScope>? scopes))
		{
			return;
		}

		scopes.Remove(scope);
		if (scopes.Count == 0)
		{
			ActiveScopes.Remove(scope.Card);
		}
	}

	private static void SetCardPlayTarget(CardPlay cardPlay, Creature? target) =>
		CardPlayTargetProperty?.SetValue(cardPlay, target);
}
