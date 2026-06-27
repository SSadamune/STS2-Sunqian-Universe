using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 保留。每回合开始时随机将本战耗能设为 0~2（参考 Slither 的
/// <see cref="CardEnergyCost.SetThisCombat"/> + 随机耗能动画）。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "finger_snap_strike")]
public sealed class FingerSnapStrike : ModCardTemplate
{
	public const string MinCostVarName = "MinCost";
	public const string MaxCostVarName = "MaxCost";

	public const int RandomCostInclusiveMax = 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(10m, ValueProp.Move),
		new DynamicVar(MinCostVarName, 0),
		new DynamicVar(MaxCostVarName, RandomCostInclusiveMax),
	];

	protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Retain,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/FingerSnapStrike.png");

	public FingerSnapStrike()
		: base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}

	/// <summary>回合开始抽牌前，为手牌中的本牌随机耗能。</summary>
	public override Task BeforeHandDraw(
		Player player,
		PlayerChoiceContext choiceContext,
		ICombatState combatState)
	{
		if (player != Owner || Pile?.Type != PileType.Hand)
		{
			return Task.CompletedTask;
		}

		RandomizeEnergyCostThisCombat();
		return Task.CompletedTask;
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(4m);
	}

	private void RandomizeEnergyCostThisCombat()
	{
		int cost = Owner.RunState.Rng.CombatEnergyCosts.NextInt(RandomCostInclusiveMax + 1);
		EnergyCost.SetThisCombat(cost);
		NCard.FindOnTable(this)?.PlayRandomizeCostAnim();
	}
}
