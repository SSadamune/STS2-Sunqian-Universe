using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
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
/// 杀意感知：攻击目标后，令盟友从手牌中选择至多一张可打出的攻击牌免费自动打出。
/// 上将军会使这些跨玩家自动打出的攻击牌继承本牌实际消耗的活力加成。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "killing_intent_perception")]
public sealed class KillingIntentPerception : ModCardTemplate
{
	public const int BaseDamage = 9;
	public const int UpgradedDamage = 13;

	private static readonly LocString SelectionPrompt =
		new("cards", "SUNQIAN_UNIVERSE_CARD_KILLING_INTENT_PERCEPTION.selectionScreenPrompt");

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

		List<Player> allies = combatState.Players.Where(player =>
			player != Owner
			&& !player.Creature.IsDead)
			.ToList();

		if (!IsUpgraded)
		{
			List<Player> eligibleAllies = allies
				.Where(player => GetPlayableAttacks(player).Count > 0)
				.ToList();
			if (eligibleAllies.Count == 0)
			{
				return;
			}

			Player? ally = Owner.RunState.Rng.CombatCardSelection
				.NextItem(eligibleAllies);
			if (ally is null)
			{
				return;
			}

			await ChooseAndAutoPlayAttack(choiceContext, ally, target);
			return;
		}

		foreach (Player ally in allies)
		{
			if (!target.IsAlive)
			{
				break;
			}

			if (ally.Creature.IsDead)
			{
				continue;
			}

			if (GetPlayableAttacks(ally).Count == 0)
			{
				continue;
			}

			await ChooseAndAutoPlayAttack(choiceContext, ally, target);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - BaseDamage);
	}

	private static List<CardModel> GetPlayableAttacks(Player player) =>
		PileType.Hand.GetPile(player).Cards
			.Where(IsPlayableAttack)
			.ToList();

	private static bool IsPlayableAttack(CardModel card) =>
		card.Type == CardType.Attack && CanAutoPlayForFree(card);

	private async Task ChooseAndAutoPlayAttack(
		PlayerChoiceContext choiceContext,
		Player ally,
		Creature target)
	{
		var prefs = new CardSelectorPrefs(SelectionPrompt, minCount: 0, maxCount: 1)
		{
			RequireManualConfirmation = true,
		};
		CardModel? selectedAttack = (await CardSelectCmd.FromHand(
			choiceContext,
			ally,
			prefs,
			IsPlayableAttack,
			this)).FirstOrDefault();
		if (selectedAttack is not null)
		{
			await CardCmd.AutoPlay(choiceContext, selectedAttack, target);
		}
	}

	private static bool CanAutoPlayForFree(CardModel card)
	{
		card.CanPlay(out UnplayableReason reason, out _);
		const UnplayableReason ignoredResourceCosts =
			UnplayableReason.EnergyCostTooHigh | UnplayableReason.StarCostTooHigh;
		return (reason & ~ignoredResourceCosts) == UnplayableReason.None;
	}
}
