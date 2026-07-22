using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 大义灭亲：消耗抽牌堆中过去 5 场战斗打出率最高的 X 张牌（率相同则优先打出次数更多者）；每消耗一张造成伤害并获得力量。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "loyalty_over_kin")]
public sealed class LoyaltyOverKin : ModCardTemplate
{
	public const int DamageAmount = 6;

	public const int BaseStrength = 2;

	public const int UpgradedStrength = 3;

	public const int PlayRateWindow = CardDrawPlayRateTracker.DefaultWindowSize;

	protected override bool HasEnergyCostX => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(DamageAmount, ValueProp.Move),
		new PowerVar<StrengthPower>(BaseStrength),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Ethereal,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/LoyaltyOverKin.png");

	public LoyaltyOverKin()
		: base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		int count = ResolveEnergyXValue();
		if (count <= 0)
		{
			return;
		}

		const bool includeCurrentCombat = false;
		CardDrawPlayRateTracker.LogCurrentState(
			Owner,
			windowSize: PlayRateWindow,
			includeCurrentCombat: includeCurrentCombat,
			reason: $"Loyalty Over Kin OnPlay (X={count}, before selection)");

		List<CardModel> selected = CardDrawPlayRateTracker.SelectHighestPlayRateFromDrawPile(
			Owner,
			count,
			windowSize: PlayRateWindow,
			includeCurrentCombat: includeCurrentCombat,
			rng: Owner.RunState.Rng.CombatCardGeneration);

		CardDrawPlayRateTracker.LogCurrentState(
			Owner,
			windowSize: PlayRateWindow,
			includeCurrentCombat: includeCurrentCombat,
			selectedCards: selected,
			reason: $"Loyalty Over Kin OnPlay (X={count}, after selection)");

		Creature target = cardPlay.Target;
		decimal strengthAmount = DynamicVars[nameof(StrengthPower)].BaseValue;
		SquVigorSnapshot.AttackSequence vigorSequence = SquVigorSnapshot.BeginAttackSequence(Owner.Creature, this);

		foreach (CardModel card in selected)
		{
			if (card.Pile?.Type != PileType.Draw)
			{
				continue;
			}

			await CardCmd.Exhaust(choiceContext, card);

			if (target.IsAlive)
			{
				await DamageCmd.Attack(vigorSequence.ResolveNextAttackDamage())
					.FromCard(this, cardPlay)
					.Targeting(target)
					.WithHitFx("vfx/vfx_attack_slash")
					.Execute(choiceContext);
			}

			await PowerCmd.Apply<StrengthPower>(
				choiceContext,
				Owner.Creature,
				strengthAmount,
				Owner.Creature,
				this);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(StrengthPower)].UpgradeValueBy(UpgradedStrength - BaseStrength);
	}
}
