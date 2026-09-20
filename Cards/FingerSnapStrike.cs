using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Audio;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 保留。蓄能：被保留时将本战耗能随机改为与当前不同的 0~2（参考 Slither 的
/// <see cref="CardEnergyCost.SetThisCombat"/> + 随机耗能动画）。打出后解除。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "finger_snap_strike")]
public sealed class FingerSnapStrike : ChargeCardTemplate
{
	public const int BaseDamage = 11;
	public const int UpgradedDamage = 15;
	public const string MinCostVarName = "MinCost";
	public const string MaxCostVarName = "MaxCost";

	public const int RandomCostInclusiveMax = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
		new DynamicVar(MinCostVarName, 0),
		new DynamicVar(MaxCostVarName, RandomCostInclusiveMax),
	];

	protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Retain,
		.. base.CanonicalKeywords,
	];

	protected override string ChargeEffectLocKey => Id.Entry + ".chargeEffect";

	protected override ChargeHooks Charge => new(
		OnRetained: RandomizeEnergyCostUntilPlayed,
		OnPowerAmountChanged: null,
		Clear: ResetEnergyCost);

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/FingerSnapStrike.png");

	public FingerSnapStrike()
		: base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		SquSfx.PlayRandom(
			RunState,
			SquSfx.FingerStrikeDongZhuoHeadEvent,
			SquSfx.FingerStrikeRemainingLandEvent);
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - BaseDamage);
	}

	private Task RandomizeEnergyCostUntilPlayed(PlayerChoiceContext choiceContext)
	{
		int currentCost = EnergyCost.GetResolved();
		int cost = Owner.RunState.Rng.CombatEnergyCosts.NextInt(
			currentCost is >= 0 and <= RandomCostInclusiveMax
				? RandomCostInclusiveMax
				: RandomCostInclusiveMax + 1);

		if (currentCost is >= 0 and <= RandomCostInclusiveMax && cost >= currentCost)
		{
			cost++;
		}

		EnergyCost.SetThisCombat(cost);
		NCard.FindOnTable(this)?.PlayRandomizeCostAnim();
		return Task.CompletedTask;
	}

	private void ResetEnergyCost()
	{
		EnergyCost.SetThisCombat(EnergyCost.Canonical);
	}
}
