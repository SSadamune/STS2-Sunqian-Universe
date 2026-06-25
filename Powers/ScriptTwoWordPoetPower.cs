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
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class ScriptTwoWordPoetPower : ScriptPowerTemplate
{
	public const string SnapshotDamageVarName = "SnapshotDamage";

	public const string TargetingDescriptionVarName = "TargetingDescription";

	private sealed class Data
	{
		public decimal SnapshotDamagePerHit;

		public int HitCount;

		public bool TargetsAllEnemies;

		public CardModel? SourceCard;
	}

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptTwoWordPoetPower.png",
		BigIconPath: "res://images/powers/ScriptTwoWordPoetPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(SnapshotDamageVarName, 0m, ValueProp.Move),
		new RepeatVar(2),
		new StringVar(TargetingDescriptionVarName, "随机对敌人"),
	];

	protected override object InitInternalData() => new Data();

	public static decimal CalculateSnapshotDamagePerHit(CardModel card, Creature dealer)
	{
		DamageVar damageVar = card.DynamicVars.Damage;
		return Hook.ModifyDamage(
			card.Owner.RunState,
			card.CombatState,
			null,
			dealer,
			damageVar.BaseValue,
			damageVar.Props,
			card,
			ModifyDamageHookType.All,
			CardPreviewMode.None,
			out _);
	}

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		Data data = GetInternalData<Data>();
		data.SnapshotDamagePerHit = Amount;
		data.HitCount = cardSource is TwoWordPoetScript script
			? script.DynamicVars.Repeat.IntValue
			: 2;
		data.TargetsAllEnemies = cardSource is TwoWordPoetScript { IsUpgraded: true };
		data.SourceCard = cardSource;

		DynamicVars[SnapshotDamageVarName].BaseValue = data.SnapshotDamagePerHit;
		DynamicVars.Repeat.BaseValue = data.HitCount;
		((StringVar)DynamicVars[TargetingDescriptionVarName]).StringValue =
			data.TargetsAllEnemies ? "对所有敌人" : "随机对敌人";
		return Task.CompletedTask;
	}

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (side != Owner.Side || !participants.Contains(Owner) || Owner.IsDead)
		{
			return;
		}

		Data data = GetInternalData<Data>();
		if (data.SnapshotDamagePerHit <= 0m || data.SourceCard is not CardModel sourceCard)
		{
			return;
		}

		Flash();

		AttackCommand attack = DamageCmd.Attack(data.SnapshotDamagePerHit)
			.Unpowered()
			.WithHitCount(data.HitCount)
			.FromCard(sourceCard)
			.WithHitFx("vfx/vfx_attack_slash");

		if (data.TargetsAllEnemies)
		{
			await attack.TargetingAllOpponents(combatState)
				.Execute(new ThrowingPlayerChoiceContext());
		}
		else
		{
			await attack.TargetingRandomOpponents(combatState)
				.Execute(new ThrowingPlayerChoiceContext());
		}
	}
}
