#nullable enable
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using Squ.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

[RegisterPower]
public sealed class LifeContractPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Single;
	public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;
	public override int DisplayAmount => DynamicVars["Healing"].IntValue;
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Healing", 0)];
	protected override string SmartDescriptionLocKey => IsMutable && CombatManager.Instance.IsInProgress
		? Id.Entry + ".combatSmartDescription" : base.SmartDescriptionLocKey;
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/LifeContractPower.png",
		BigIconPath: "res://images/powers/LifeContractPowerBig.png");
	private string CompanionId => Owner.Monster is ThirdBrother ? CompanionSummons.ThirdBrotherId : CompanionSummons.SecondBrotherId;
	public override LocString Description
	{
		get { var text = base.Description; DynamicVars.AddTo(text); return text; }
	}
	public void Refresh()
	{
		DynamicVars["Healing"].BaseValue = Owner.PetOwner is { } player ? LifeContractSystem.GetTotal(player, CompanionId) : 0;
		InvokeDisplayAmountChanged();
	}
	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		Refresh();
		return Task.CompletedTask;
	}
	public override async Task AfterCombatEnd(CombatRoom room)
	{
		if (Owner.IsAlive && Owner.PetOwner is { Creature.IsAlive: true } player)
		{
			int healing = Math.Min(Owner.CurrentHp, LifeContractSystem.Claim(player, CompanionId));
			if (healing > 0)
			{
				Flash();
				await CreatureCmd.Heal(player.Creature, healing, playAnim: false);
			}
		}
	}
}
