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
using Squ.Combat;
using Squ.Interop;
using Squ.Relics;
using STS2RitsuLib.Combat.HealthBars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// Burning: stackable debuff that deals damage equal to its stacks, then may
/// extinguish itself with an escalating chance. Enemies trigger it at turn
/// start; players and pets trigger it at the end of their owner's turn.
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

	protected override string SmartDescriptionLocKey =>
		IsMutable && Owner is { } owner && (owner.IsPlayer || owner.IsPet)
			? Id.Entry + ".playerSmartDescription"
			: base.SmartDescriptionLocKey;

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
		ReduceClearChanceTo(InitialClearChance);
		AdvancedGunpowderStudiesRelic.TryApplyZeroClearChance(applier ?? Applier, this);
		return Task.CompletedTask;
	}

	public override Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		// Hook broadcasts to every combat listener; only react to our own stacks being increased.
		if (power == this && amount > 0m)
		{
			ReduceClearChanceTo(InitialClearChance);
			AdvancedGunpowderStudiesRelic.TryApplyZeroClearChance(applier ?? Applier, this);
		}

		return Task.CompletedTask;
	}

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (!Owner.IsEnemy || Owner.IsPet || !participants.Contains(Owner))
		{
			return;
		}

		await TriggerAsync(new ThrowingPlayerChoiceContext());
	}

	public override async Task AfterSideTurnEnd(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IEnumerable<Creature> participants)
	{
		Creature? turnOwner = Owner.IsPlayer ? Owner : Owner.PetOwner?.Creature;
		if (side != CombatSide.Player
			|| turnOwner is null
			|| !participants.Contains(turnOwner))
		{
			return;
		}

		await TriggerAsync(choiceContext);
	}

	/// <summary>
	/// Resolves one complete Burning trigger, including damage, extinguish
	/// prevention/checking, and the post-trigger chance adjustment. The
	/// adjustment may be negative and is clamped to the 0–1 range.
	/// </summary>
	public async Task<bool> TriggerAsync(
		PlayerChoiceContext choiceContext,
		bool shouldCheckExtinguish = true,
		float clearChanceIncrement = ClearChanceIncrement,
		CardModel? cardSource = null)
	{
		if (Owner.IsDead || Amount <= 0 || Owner.CombatState is not { } combatState)
		{
			return false;
		}
		if (!float.IsFinite(clearChanceIncrement))
		{
			throw new ArgumentOutOfRangeException(
				nameof(clearChanceIncrement),
				"The extinguish chance increment must be finite.");
		}

		Flash();

		PlayFiendFireDamageVfx(Owner);

		IronChainPower? ironChain = Owner.GetPower<IronChainPower>();
		IEnumerable<DamageResult> damageResults = await CreatureCmd.Damage(
			choiceContext,
			Owner,
			Amount,
			ValueProp.Unpowered,
			cardSource,
			null);

		if (ironChain is not null)
		{
			int damageDealt = damageResults.Sum(result => result.TotalDamage);
			await ironChain.AfterBurningDamage(damageDealt);
		}

		if (!Owner.IsAlive)
		{
			return true;
		}

		Data data = GetInternalData<Data>();
		if (shouldCheckExtinguish)
		{
			bool prevented = await UnextinguishedPower.TryPreventExtinguish(Owner);
			if (!prevented)
			{
				Rng rng = combatState.RunState.Rng.Niche;
				bool rolledExtinguish = data.ClearChance >= 1f
					|| rng.NextFloat() < data.ClearChance;
				if (rolledExtinguish)
				{
					decimal extinguishedAmount = Amount;
					await PowerCmd.Remove(this);
					await GreatWeiChoirPower.OnBurningExtinguishedAsync(
						choiceContext,
						combatState,
						extinguishedAmount,
						cardSource);
					return true;
				}
			}
		}

		data.ClearChance = Math.Clamp(
			data.ClearChance + clearChanceIncrement,
			0f,
			1f);
		SyncClearChanceVar();
		return true;
	}

	/// <summary>
	/// Lowers the extinguish chance used after the next trigger (0–1). Never raises it.
	/// </summary>
	public void ReduceClearChanceTo(float clearChance)
	{
		Data data = GetInternalData<Data>();
		float capped = Math.Clamp(clearChance, 0f, 1f);
		if (capped >= data.ClearChance)
		{
			return;
		}

		data.ClearChance = capped;
		SyncClearChanceVar();
	}

	private void SyncClearChanceVar()
	{
		float clearChance = GetInternalData<Data>().ClearChance;
		DynamicVars[ClearChanceVarName].BaseValue = (decimal)Math.Round(clearChance * 100f);
	}

	public IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(
		HealthBarForecastContext context)
	{
		if (!DefenseAwareDebuffPreviewInterop.TryGetPreviewHpLoss(
			context.Creature,
			this,
			out int hpLoss))
		{
			// Match vanilla Poison's basic forecast when the optional defense-aware
			// calculator is unavailable: show stacks without simulating defenses.
			hpLoss = Amount;
		}

		return HealthBarForecasts.Single(
			hpLoss,
			BurningLethalColor,
			HealthBarForecastGrowthDirection.FromRight,
			OrderedPoisonHealthBarForecastSource.GetTriggerOrder(context.Creature, this));
	}

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
