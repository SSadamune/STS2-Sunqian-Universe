using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
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
public sealed class ScriptTwoWordPoetPower : StackableScriptPowerTemplate
{
	public const string SnapshotDamageVarName = "SnapshotDamage";

	public const string TargetingDescriptionVarName = "TargetingDescription";

	public const int TurnStartHitCount = 2;

	private sealed class Data
	{
		public bool TargetsAllEnemies;

		public CardModel? SourceCard;
	}

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptTwoWordPoetPower.png",
		BigIconPath: "res://images/powers/ScriptTwoWordPoetPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(SnapshotDamageVarName, 0m, ValueProp.Move),
		new RepeatVar(TurnStartHitCount),
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

	protected override void OnStackedFrom(CardModel? cardSource)
	{
		if (cardSource is not TwoWordPoetScript script)
		{
			return;
		}

		Data data = GetInternalData<Data>();
		MergeUpgradedFlag(ref data.TargetsAllEnemies, script);
		data.SourceCard = PreferUpgradedSourceCard(data.SourceCard, cardSource);
		SyncDisplayVars(data);
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
		if (Amount <= 0m || data.SourceCard is not CardModel sourceCard)
		{
			return;
		}

		Flash();

		AttackCommand attack = DamageCmd.Attack(Amount)
			.Unpowered()
			.WithHitCount(TurnStartHitCount)
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

	private void SyncDisplayVars(Data data)
	{
		DynamicVars[SnapshotDamageVarName].BaseValue = Amount;
		DynamicVars.Repeat.BaseValue = TurnStartHitCount;
		((StringVar)DynamicVars[TargetingDescriptionVarName]).StringValue =
			data.TargetsAllEnemies ? "对所有敌人" : "随机对敌人";
	}
}
