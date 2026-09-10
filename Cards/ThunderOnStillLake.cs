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
using Squ;
using Squ.Audio;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 平湖惊雷：保留。对所有敌人造成伤害（未升级 1 费 4，升级后 5 费 3）。
/// 蓄能：回合结束降费；消耗活力后增加等量伤害。打出后解除。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "thunder_on_still_lake")]
public sealed class ThunderOnStillLake : ChargeCardTemplate
{
	public const decimal CanonicalDamage = 1m;

	public const decimal UpgradedDamage = 7m;

	public const int CanonicalCost = 4;

	public const int UpgradedCost = 3;

	public const int EnergyReductionPerTurn = 1;

	private const decimal ChopYourHeadDamage = 30m;

	private const decimal AsLongAsIBreatheDamage = 10m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(CanonicalDamage, ValueProp.Move),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Retain,
		.. base.CanonicalKeywords,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
	];

	protected override string ChargeEffectLocKey => Id.Entry + ".chargeEffect";

	protected override ChargeHooks Charge => new(
		OnTurnEndInHand: ReduceCostUntilPlayed,
		OnPowerAmountChanged: GainDamageFromSpentVigor,
		Clear: ResetDamageUntilPlayed);

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/ThunderOnStillLake.png");

	public ThunderOnStillLake()
		: base(CanonicalCost, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(CombatState, nameof(CombatState));

		PlayOnPlaySfx();
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.TargetingAllOpponents(CombatState)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}

	private void PlayOnPlaySfx()
	{
		decimal damage = DynamicVars.Damage.BaseValue;
		if (damage >= ChopYourHeadDamage)
		{
			SquSfx.Play(SquSfx.ThunderOnStillLakeChopYourHeadEvent);
			return;
		}

		if (damage >= AsLongAsIBreatheDamage && EnergyCost.GetResolved() == 0)
		{
			SquSfx.Play(SquSfx.ThunderOnStillLakeAsLongAsIBreatheEvent);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - CanonicalDamage);
		EnergyCost.UpgradeBy(UpgradedCost - CanonicalCost);
	}

	private Task ReduceCostUntilPlayed(PlayerChoiceContext choiceContext)
	{
		EnergyCost.AddUntilPlayed(-EnergyReductionPerTurn);
		InvokeEnergyCostChanged();
		return Task.CompletedTask;
	}

	private Task GainDamageFromSpentVigor(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (power is not VigorPower || power.Owner != Owner.Creature || amount >= 0m)
		{
			return Task.CompletedTask;
		}

		DynamicVars.Damage.BaseValue += -amount;
		return Task.CompletedTask;
	}

	private void ResetDamageUntilPlayed()
	{
		DynamicVars.Damage.BaseValue = PrintedDamage;
	}

	private decimal PrintedDamage => IsUpgraded ? UpgradedDamage : CanonicalDamage;
}
