using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// Stores a mount's bound enemy and relays every point of HP loss, including lethal overkill and
/// direct-kill remaining HP, at the configured multiplier.
/// </summary>
[RegisterPower]
public sealed class MutatedCentaurLinkPower : ModPowerTemplate
{
	private static readonly PowerModel MinionPowerTemplate = ModelDb.Power<MinionPower>();

	private Creature? _boundEnemy;
	private string _boundEnemyName = string.Empty;
	private decimal _damageMultiplier = 1m;
	private decimal _fractionalDamageCarry;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public override bool ShouldPlayVfx => false;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: MinionPowerTemplate.PackedIconPath,
		BigIconPath: MinionPowerTemplate.ResolvedBigIconPath);

	public override LocString Description
	{
		get
		{
			LocString description = base.Description;
			description.Add("EnemyName", _boundEnemyName);
			description.Add("DamagePercent", _damageMultiplier * 100m);
			return description;
		}
	}

	internal void Bind(Creature boundEnemy, decimal damageMultiplier)
	{
		_boundEnemy = boundEnemy;
		_boundEnemyName = boundEnemy.Name;
		_damageMultiplier = damageMultiplier;
	}

	public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		if (creature == Owner && delta < 0m)
		{
			await RelayDamage(new ThrowingPlayerChoiceContext(), -delta);
		}
	}

	public override async Task AfterDamageGiven(
		PlayerChoiceContext choiceContext,
		Creature? dealer,
		DamageResult result,
		ValueProp props,
		Creature target,
		CardModel? cardSource)
	{
		if (target == Owner && result.OverkillDamage > 0)
		{
			await RelayDamage(choiceContext, result.OverkillDamage);
		}
	}

	public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;

	public override bool ShouldOwnerDeathTriggerFatal() => false;

	private async Task RelayDamage(PlayerChoiceContext choiceContext, decimal mountHpLost)
	{
		if (mountHpLost <= 0m || _boundEnemy is not { IsAlive: true } boundEnemy)
		{
			return;
		}

		decimal scaledDamage = mountHpLost * _damageMultiplier + _fractionalDamageCarry;
		int damage = (int)decimal.Floor(scaledDamage);
		_fractionalDamageCarry = scaledDamage - damage;
		if (damage <= 0)
		{
			return;
		}

		Flash();
		await CreatureCmd.Damage(
			choiceContext,
			boundEnemy,
			damage,
			ValueProp.Unblockable | ValueProp.Unpowered,
			null,
			null);
	}
}
