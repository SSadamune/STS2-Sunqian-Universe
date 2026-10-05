using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 杀意感知：攻击目标后，令已结束回合的盟友从手牌中随机自动打出一张可打出的攻击牌。
/// 上将军会使这些跨玩家自动打出的攻击牌继承本牌实际消耗的活力加成。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "killing_intent_perception")]
public sealed class KillingIntentPerception : ModCardTemplate
{
	public const int BaseDamage = 9;
	public const int UpgradedDamage = 13;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.SupremeGeneral,
	];

	public override CardMultiplayerConstraint MultiplayerConstraint =>
		CardMultiplayerConstraint.MultiplayerOnly;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/KillingIntentPerception.png");

	public KillingIntentPerception()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
		ICombatState combatState = CombatState
			?? throw new InvalidOperationException(
				"KillingIntentPerception requires an active combat.");
		Creature target = cardPlay.Target;

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);

		if (!target.IsAlive)
		{
			return;
		}

		List<Player> eligibleAllies = combatState.Players.Where(player =>
			player != Owner
			&& !player.Creature.IsDead
			&& CombatManager.Instance.IsPlayerReadyToEndTurn(player))
			.ToList();
		if (!IsUpgraded)
		{
			eligibleAllies = eligibleAllies.Take(1).ToList();
		}

		foreach (Player ally in eligibleAllies)
		{
			List<CardModel> playableAttacks = PileType.Hand.GetPile(ally).Cards
				.Where(card => card.Type == CardType.Attack && CanAutoPlayForFree(card))
				.ToList();
			if (playableAttacks.Count == 0)
			{
				continue;
			}

			CardModel? attack = Owner.RunState.Rng.CombatCardSelection
				.NextItem(playableAttacks);
			if (attack is not null)
			{
				await CardCmd.AutoPlay(choiceContext, attack, target);
			}
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - BaseDamage);
	}

	private static bool CanAutoPlayForFree(CardModel card)
	{
		card.CanPlay(out UnplayableReason reason, out _);
		const UnplayableReason ignoredResourceCosts =
			UnplayableReason.EnergyCostTooHigh | UnplayableReason.StarCostTooHigh;
		return (reason & ~ignoredResourceCosts) == UnplayableReason.None;
	}
}
