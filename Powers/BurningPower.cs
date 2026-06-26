using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.HealthBars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// Burning: stackable debuff that deals damage equal to its stacks at turn start,
/// then may clear itself with an escalating chance.
/// </summary>
[RegisterPower]
public sealed class BurningPower : ModPowerTemplate, IHealthBarForecastSource
{
	private static readonly Color BurningLethalColor = new("FFB733");
	public const string ClearChanceVarName = "ClearChance";

	public const float InitialClearChance = 0.25f;

	public const float ClearChanceIncrement = 0.25f;

	private sealed class Data
	{
		public float ClearChance = InitialClearChance;
	}

	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/BurningPower.png",
		BigIconPath: "res://images/powers/BurningPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(ClearChanceVarName, (decimal)(InitialClearChance * 100f)),
	];

	/// <summary>
	/// Card hand hovers use <see cref="PowerModel.GetDumbHoverTip" />, which reads
	/// <see cref="Description" /> but does not call <c>DynamicVars.AddTo</c> itself.
	/// </summary>
	public override LocString Description
	{
		get
		{
			LocString description = base.Description;
			DynamicVars.AddTo(description);
			return description;
		}
	}

	protected override object InitInternalData() => new Data();

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		SyncClearChanceVar();
		return Task.CompletedTask;
	}

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (!participants.Contains(Owner) || Owner.IsDead || Amount <= 0)
		{
			return;
		}

		Flash();

		PlayFiendFireDamageVfx(Owner);

		await CreatureCmd.Damage(
			new ThrowingPlayerChoiceContext(),
			Owner,
			Amount,
			ValueProp.Unblockable | ValueProp.Unpowered,
			Applier,
			null);

		if (!Owner.IsAlive)
		{
			return;
		}

		Data data = GetInternalData<Data>();
		Rng rng = combatState.RunState.Rng.Niche;
		bool cleared = data.ClearChance >= 1f || rng.NextFloat() < data.ClearChance;
		if (cleared)
		{
			await PowerCmd.Remove(this);
			return;
		}

		data.ClearChance = Math.Min(data.ClearChance + ClearChanceIncrement, 1f);
		SyncClearChanceVar();
	}

	private void SyncClearChanceVar()
	{
		float clearChance = GetInternalData<Data>().ClearChance;
		DynamicVars[ClearChanceVarName].BaseValue = (decimal)Math.Round(clearChance * 100f);
	}

	public IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context) =>
		HealthBarForecasts.Single(
			Amount,
			BurningLethalColor,
			HealthBarForecastGrowthDirection.FromRight);

	private static void PlayFiendFireDamageVfx(Creature target)
	{
		NGroundFireVfx? vfx = NGroundFireVfx.Create(target);
		Node? container = NCombatRoom.Instance?.CombatVfxContainer;
		if (vfx == null || container == null)
		{
			return;
		}

		SfxCmd.Play("event:/sfx/characters/attack_fire");
		container.AddChildSafely((Node)vfx);
	}
}
