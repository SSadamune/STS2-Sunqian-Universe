#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

/// <summary>
/// 倾巢而出：若本回合未打出能获得格挡的技能牌，回合结束时对随机非爪牙敌人造成
/// 本回合累计伤害乘以 <see cref="Amount"/>% 的伤害。倍率可叠加。
/// </summary>
[RegisterPower]
public sealed class AllInPower : ModPowerTemplate
{
	public const decimal BaseBonusPercent = 50m;
	public const decimal UpgradedBonusPercent = 75m;
	public const string TurnDamageVarName = "TurnDamage";

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public bool Blocked => IsMutable && AllInTurnTracker.PlayedBlockSkill(Owner.Player);

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(TurnDamageVarName, 0m),
	];

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/AllInPower.png",
		BigIconPath: "res://images/powers/AllInPowerBig.png");

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		SyncTurnDamageVar();
		return Task.CompletedTask;
	}

	public override Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (power == this)
		{
			SyncTurnDamageVar();
		}

		return Task.CompletedTask;
	}

	public override async Task AfterSideTurnEndLate(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IEnumerable<Creature> participants)
	{
		if (side != Owner.Side || !participants.Contains(Owner) || Amount <= 0m)
		{
			return;
		}

		Player? player = Owner.Player;
		if (player is null)
		{
			return;
		}

		decimal damage = CalculateEndTurnDamage();
		if (!AllInTurnTracker.PlayedBlockSkill(player) && damage > 0m)
		{
			List<Creature> targets = Owner.CombatState!.HittableEnemies
				.Where(enemy => enemy.IsAlive && !enemy.HasPower<MinionPower>())
				.ToList();
			Creature? target = player.RunState.Rng.CombatTargets.NextItem(targets);
			if (target is not null)
			{
				Flash();
				await CreatureCmd.Damage(
					choiceContext,
					target,
					damage,
					ValueProp.Unpowered,
					Owner,
					cardSource: null,
					cardPlay: null);
			}
		}

		AllInTurnTracker.Reset(player);
	}

	internal decimal CalculateEndTurnDamage() =>
		Math.Floor(AllInTurnTracker.GetTotalDamage(Owner.Player) * Amount / 100m);

	internal void SyncTurnDamageVar()
	{
		DynamicVars[TurnDamageVarName].BaseValue = CalculateEndTurnDamage();
	}
}
