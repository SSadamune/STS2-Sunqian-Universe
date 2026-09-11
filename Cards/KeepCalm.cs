using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 保持冷静：保留。打出获得当前积攒的能量（印面始终 1）。
/// 被保留时能量 +1（升级 +2）。受到未被格挡的攻击伤害时能量变为 0，
/// 并按损失的能量加入等量未升级愤怒。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "keep_calm")]
public sealed class KeepCalm : ChargeCardTemplate
{
	public const string RetainBonusVarName = "RetainBonus";

	public const int CanonicalEnergy = 1;
	public const int CanonicalRetainBonus = 1;
	public const int UpgradedRetainBonus = 2;

	private bool _resolvingUnblockedHit;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(CanonicalEnergy),
		new DynamicVar(RetainBonusVarName, CanonicalRetainBonus),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Retain,
		.. base.CanonicalKeywords,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.ForEnergy(this),
		HoverTipFactory.FromCard<Anger>(),
	];

	protected override string ChargeEffectLocKey => Id.Entry + ".chargeEffect";

	protected override ChargeHooks Charge => new(
		OnRetained: IncreaseEnergyUntilPlayed,
		OnPowerAmountChanged: null,
		Clear: ResetEnergy);

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/KeepCalm.png");

	protected override bool ShouldGlowGoldInternal => DynamicVars.Energy.IntValue > CanonicalEnergy;

	public KeepCalm()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		int energy = DynamicVars.Energy.IntValue;
		if (energy > 0)
		{
			await PlayerCmd.GainEnergy(energy, Owner);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars[RetainBonusVarName]
			.UpgradeValueBy(UpgradedRetainBonus - CanonicalRetainBonus);
	}

	public override async Task AfterDamageReceived(
		PlayerChoiceContext choiceContext,
		Creature target,
		DamageResult result,
		ValueProp props,
		Creature? dealer,
		CardModel? cardSource)
	{
		if (_resolvingUnblockedHit
			|| Pile?.Type != PileType.Hand
			|| target != Owner.Creature
			|| !props.IsPoweredAttack()
			|| result.UnblockedDamage <= 0)
		{
			return;
		}

		int energyLost = DynamicVars.Energy.IntValue;
		if (energyLost <= 0)
		{
			return;
		}

		_resolvingUnblockedHit = true;
		try
		{
			DynamicVars.Energy.BaseValue = 0m;
			await AddAngerAsync(energyLost);
		}
		finally
		{
			_resolvingUnblockedHit = false;
		}
	}

	private Task IncreaseEnergyUntilPlayed(PlayerChoiceContext choiceContext)
	{
		DynamicVars.Energy.BaseValue += DynamicVars[RetainBonusVarName].IntValue;
		return Task.CompletedTask;
	}

	private async Task AddAngerAsync(int energyLost)
	{
		ICombatState combatState = CombatState
			?? throw new InvalidOperationException("KeepCalm requires an active combat.");

		for (int i = 0; i < energyLost; i++)
		{
			await GeneratedCombatCards.AddToHandInCombat<Anger>(
				combatState,
				Owner,
				upgraded: false,
				Owner);
		}
	}

	private void ResetEnergy()
	{
		DynamicVars.Energy.BaseValue = CanonicalEnergy;
	}
}
