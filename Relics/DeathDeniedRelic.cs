#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using Squ.Character;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Relics;

/// <summary>
/// 求死不得：累计己方回合内的生命损失和格挡伤害，未被格挡的伤害仅通过生命变化计数。
/// 致死触发后，本场战斗不再记录或结算。
/// </summary>
[RegisterRelic(typeof(LiubeiRelicPool), StableEntryStem = "death_denied")]
[RegisterCharacterStarterRelic(typeof(LiubeiCharacter))]
public sealed class DeathDeniedRelic : ModRelicTemplate
{
	private const string RecoveryPercentKey = "RecoveryPercent";

	private int _hpLostThisCombat;
	private bool _disabledThisCombat;

	public override RelicRarity Rarity => RelicRarity.Starter;

	public override bool IsUsedUp => DisabledThisCombat;

	public override bool ShowCounter =>
		CombatManager.Instance.IsInProgress
		&& !DisabledThisCombat
		&& RecoverableHealth > 0;

	public override int DisplayAmount => RecoverableHealth;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(RecoveryPercentKey, 50m),
	];

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/DeathDeniedRelic.png",
		IconOutlinePath: "res://images/relics/DeathDeniedRelicOutline.png",
		BigIconPath: "res://images/relics/DeathDeniedRelicBig.png");

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int HpLostThisCombat
	{
		get => _hpLostThisCombat;
		set
		{
			AssertMutable();
			_hpLostThisCombat = Math.Max(value, 0);
			InvokeDisplayAmountChanged();
			if (!DisabledThisCombat)
			{
				Status = RecoverableHealth > 0 ? RelicStatus.Active : RelicStatus.Normal;
			}
		}
	}

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool DisabledThisCombat
	{
		get => _disabledThisCombat;
		set
		{
			AssertMutable();
			_disabledThisCombat = value;
			Status = value
				? RelicStatus.Disabled
				: RecoverableHealth > 0 ? RelicStatus.Active : RelicStatus.Normal;
			InvokeDisplayAmountChanged();
		}
	}

	internal int RecoverableHealth =>
		HpLostThisCombat * DynamicVars[RecoveryPercentKey].IntValue / 100;

	// AfterCombatEnd runs before victory healing. Keep the total and death-
	// prevention flag until victory, and reset them when the next fight starts.
	public override Task BeforeCombatStart()
	{
		HpLostThisCombat = 0;
		DisabledThisCombat = false;
		return Task.CompletedTask;
	}

	public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		if (DisabledThisCombat
			|| creature != Owner.Creature
			|| delta >= 0m
			|| !CombatManager.Instance.IsInProgress
			|| creature.CombatState?.CurrentSide != creature.Side)
		{
			return Task.CompletedTask;
		}

		HpLostThisCombat += (int)(-delta);
		return Task.CompletedTask;
	}

	public override Task AfterDamageGiven(PlayerChoiceContext context, Creature? dealer,
		DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		// HP changes already account for the unblocked portion, including temporary HP.
		if (!DisabledThisCombat && target == Owner.Creature && result.Receiver == Owner.Creature
			&& CombatManager.Instance.IsInProgress && target.CombatState?.CurrentSide == target.Side
			&& result.BlockedDamage > 0)
			HpLostThisCombat += result.BlockedDamage;
		return Task.CompletedTask;
	}

	public override bool ShouldDieLate(Creature creature) =>
		creature != Owner.Creature
		|| DisabledThisCombat
		|| RecoverableHealth <= 0;

	public override async Task AfterPreventingDeath(Creature creature)
	{
		if (creature != Owner.Creature || DisabledThisCombat)
		{
			return;
		}

		int amount = RecoverableHealth;
		if (amount <= 0)
		{
			return;
		}

		Flash();
		DisabledThisCombat = true;
		await CreatureCmd.Heal(creature, amount);
	}

	public override async Task AfterCombatVictoryEarly(CombatRoom _)
	{
		Creature creature = Owner.Creature;
		int amount = RecoverableHealth;
		if (DisabledThisCombat || creature.IsDead || amount <= 0)
		{
			return;
		}

		Flash();
		await CreatureCmd.Heal(creature, amount);
		HpLostThisCombat = 0;
	}
}
