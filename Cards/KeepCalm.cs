using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Character;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 保持冷静：保留。打出获得当前积攒的能量；蓄能时回合结束增加本牌能量数值，
/// 受到未被格挡的攻击伤害后消耗，并按积攒能量加入愤怒与狂怒。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "keep_calm")]
public sealed class KeepCalm : ChargeCardTemplate
{
	public const int CanonicalEnergy = 1;
	public const int EnergyPerTurn = 1;
	public const int RageEnergyThreshold = 3;

	private bool _resolvingUnblockedHit;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(CanonicalEnergy),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Retain,
		.. base.CanonicalKeywords,
	];

	private IHoverTip CreateAnnotationHoverTip()
	{
		LocString description = new("cards", Id.Entry + ".annotation");
		description.Add("energyPrefix", EnergyIconHelper.GetPrefix(this));
		description.Add(new IfUpgradedVar(IsUpgraded ? UpgradeDisplay.Upgraded : UpgradeDisplay.Normal));
		return new HoverTip(SquCommonL10n.AnnotationTitle(), description);
	}

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.ForEnergy(this),
		CreateAnnotationHoverTip(),
		HoverTipFactory.FromCard<Anger>(IsUpgraded),
		HoverTipFactory.FromCard<Rage>(IsUpgraded),
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
	];

	protected override string ChargeEffectLocKey => Id.Entry + ".chargeEffect";

	protected override ChargeHooks Charge => new(
		OnTurnEndInHand: IncreaseEnergyUntilPlayed,
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
		await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
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

		_resolvingUnblockedHit = true;
		try
		{
			int energyGranted = DynamicVars.Energy.IntValue;
			await CardPileCmd.Add(this, PileType.Exhaust);
			await AddAngerAndRageAsync(energyGranted);
			ResetEnergy();
		}
		finally
		{
			_resolvingUnblockedHit = false;
		}
	}

	private Task IncreaseEnergyUntilPlayed(PlayerChoiceContext choiceContext)
	{
		DynamicVars.Energy.BaseValue += EnergyPerTurn;
		return Task.CompletedTask;
	}

	private async Task AddAngerAndRageAsync(int energyGranted)
	{
		ICombatState combatState = CombatState
			?? throw new InvalidOperationException("KeepCalm requires an active combat.");

		for (int i = 0; i < energyGranted; i++)
		{
			await GeneratedCombatCards.AddToHandInCombat<Anger>(
				combatState,
				Owner,
				IsUpgraded,
				Owner);
		}

		if (energyGranted >= RageEnergyThreshold)
		{
			await GeneratedCombatCards.AddToHandInCombat<Rage>(
				combatState,
				Owner,
				IsUpgraded,
				Owner);
		}
	}

	private void ResetEnergy()
	{
		DynamicVars.Energy.BaseValue = CanonicalEnergy;
	}
}
