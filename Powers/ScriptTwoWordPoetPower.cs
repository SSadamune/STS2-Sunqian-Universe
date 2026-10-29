using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
	public const string ScriptDamageVarName = "ScriptDamage";

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
		new DamageVar(ScriptDamageVarName, TwoWordPoetScript.CanonicalDamage, ValueProp.Move),
		new RepeatVar(ActivationHitCount),
		new StringVar(TargetingDescriptionVarName, "随机对敌人"),
	];

	protected override object InitInternalData() => new Data();

	protected override Task OnScriptApplied(Creature? applier, CardModel? cardSource)
	{
		GetInternalData<Data>().TargetsAllEnemies = cardSource is TwoWordPoetScript { IsUpgraded: true };
		SyncDisplayVars(GetInternalData<Data>());
		return Task.CompletedTask;
	}

	public override async Task AfterSideTurnStart(
		CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (!participants.Contains(Owner) || Owner.IsDead) return;
		Flash();
		SquSfx.PlayRandom(combatState.RunState, SquSfx.TwoWordPoetTriggerEvents);
		SquHitVfx.SpookyScream(Owner);
		var choiceContext = new ThrowingPlayerChoiceContext();
		for (int hit = 0; hit < ActivationHitCount; hit++)
		{
			var enemies = combatState.HittableEnemies.ToList();
			if (enemies.Count == 0) break;
			if (!GetInternalData<Data>().TargetsAllEnemies)
			{
				var target = enemies[Owner.Player!.RunState.Rng.CombatTargets.NextInt(enemies.Count)];
				enemies = [target];
			}
			await SquHitVfx.BlackSmallMagicMissiles(enemies);
			await CreatureCmd.Damage(choiceContext, enemies, Amount, ValueProp.Unpowered, Owner);
		}
	}

	private void SyncDisplayVars(Data data)
	{
		DynamicVars[ScriptDamageVarName].BaseValue = Amount;
		DynamicVars.Repeat.BaseValue = ActivationHitCount;
		((StringVar)DynamicVars[TargetingDescriptionVarName]).StringValue =
			data.TargetsAllEnemies ? "对所有敌人" : "随机对敌人";
	}

	internal void SyncStoredDamage(int amount)
	{
		SetAmount(amount);
		SyncDisplayVars(GetInternalData<Data>());
	}
}
