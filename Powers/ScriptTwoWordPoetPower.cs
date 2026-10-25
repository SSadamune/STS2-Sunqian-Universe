using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using Squ.Cards;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class ScriptTwoWordPoetPower : ScriptPowerTemplate
{
	public const string SnapshotDamageVarName = "SnapshotDamage";

	public const string TargetingDescriptionVarName = "TargetingDescription";

	public const int ActivationHitCount = 2;

	private sealed class Data
	{
		public bool TargetsAllEnemies;
	}

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptTwoWordPoetPower.png",
		BigIconPath: "res://images/powers/ScriptTwoWordPoetPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(SnapshotDamageVarName, 0m, ValueProp.Move),
		new RepeatVar(ActivationHitCount),
		new StringVar(TargetingDescriptionVarName, "随机对敌人"),
	];

	protected override object InitInternalData() => new Data();

	public static decimal CalculateSnapshotDamagePerHit(CardModel card, Creature dealer)
	{
		DamageVar damageVar = card.DynamicVars.Damage;
		using IDisposable suppression = AllInResolutionTracker.SuppressBonus();
		return Hook.ModifyDamage(
			card.Owner.RunState,
			card.CombatState,
			null,
			dealer,
			damageVar.BaseValue,
			damageVar.Props,
			card,
			null,
			ModifyDamageHookType.All,
			CardPreviewMode.None,
			out _);
	}

	protected override async Task OnScriptApplied(Creature? applier, CardModel? cardSource)
	{
		if (cardSource is not TwoWordPoetScript script
			|| Owner.IsDead
			|| Owner.CombatState is not { } combatState)
		{
			return;
		}

		Data data = GetInternalData<Data>();
		data.TargetsAllEnemies = script.IsUpgraded;
		SyncDisplayVars(data);

		Flash();
		SquSfx.PlayRandom(combatState.RunState, SquSfx.TwoWordPoetTriggerEvents);
		SquHitVfx.SpookyScream(Owner);

		AttackCommand attack = DamageCmd.Attack(Amount)
			.Unpowered()
			.WithHitCount(ActivationHitCount)
			.FromCard(cardSource, null);
		if (data.TargetsAllEnemies)
		{
			attack.BeforeDamage(() =>
				SquHitVfx.BlackSmallMagicMissiles(combatState.HittableEnemies));
			await attack.TargetingAllOpponents(combatState)
				.Execute(new ThrowingPlayerChoiceContext());
		}
		else
		{
			Creature? hitTarget = null;
			attack.WithHitVfxNode(target =>
				{
					hitTarget = target;
					return null;
				})
				.BeforeDamage(() =>
				{
					if (hitTarget is not { IsAlive: true } target)
					{
						return Task.CompletedTask;
					}

					return SquHitVfx.BlackSmallMagicMissile(target);
				});
			await attack.TargetingRandomOpponents(combatState)
				.Execute(new ThrowingPlayerChoiceContext());
		}
	}

	private void SyncDisplayVars(Data data)
	{
		DynamicVars[SnapshotDamageVarName].BaseValue = Amount;
		DynamicVars.Repeat.BaseValue = ActivationHitCount;
		((StringVar)DynamicVars[TargetingDescriptionVarName]).StringValue =
			data.TargetsAllEnemies ? "对所有敌人" : "随机对敌人";
	}
}
