#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Relics;

/// <summary>
/// 求死不得：记录持有者在玩家方回合内实际失去的生命，并在胜利或首次致死时回复其中一半。
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

	public override Task AfterCombatEnd(CombatRoom _)
	{
		HpLostThisCombat = 0;
		DisabledThisCombat = false;
		return Task.CompletedTask;
	}
}
