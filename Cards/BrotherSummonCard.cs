#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Combat;
using STS2RitsuLib.Scaffolding.Content;
using Squ.Powers;

namespace Squ.Cards;

public abstract class BrotherSummonCard : SelfHarmCardTemplate
{
	protected abstract string CompanionId { get; }
	protected BrotherSummonCard(CardRarity rarity) : base(2, CardType.Attack, rarity, TargetType.AnyEnemy) { }
	protected override IEnumerable<DynamicVar> CanonicalVars =>
		[new DynamicVar("SelfHarm", 8), new SummonVar(8), new CompanionDamageVar(CompanionId, 8)];
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/PrisonerPlaceholder.png");
	protected override IEnumerable<IHoverTip> AdditionalHoverTips
	{
		get
		{
			if (SelfHarmSystem.HasSelfHarm(this)) yield return SquKeywords.CreateSelfHarmHoverTip(SelfHarmSystem.GetAmountVar(this));
			yield return CompanionSummonKeywords.NamedHoverTip(CompanionId, DynamicVars.Summon);
			LocString contract = new("powers", ModelDb.Power<LifeContractPower>().Id.Entry + ".description");
			contract.Add("Healing", Owner != null ? LifeContractSystem.GetTotal(Owner, CompanionId) : 0);
			yield return new HoverTip(ModelDb.Power<LifeContractPower>(), contract.GetFormattedText(), false);
		}
	}
	protected override Task AfterSelfHarmPaid(PlayerChoiceContext context, int amount)
	{
		LifeContractSystem.AddContribution(Owner, amount, CompanionId);
		return Task.CompletedTask;
	}
	protected override async Task PlayAfterSelfHarm(PlayerChoiceContext context, CardPlay play)
	{
		ArgumentNullException.ThrowIfNull(play.Target);
		var summoned = await CompanionSummons.SummonSpecific(context, Owner, CompanionId, DynamicVars.Summon.BaseValue, this);
		if (summoned.Creature is not { IsAlive: true } pet) return;
		await DamageCmd.Attack(DynamicVars["CompanionDamage"].BaseValue).FromCompanion(pet, this, play)
			.Targeting(play.Target).WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3").Execute(context);
	}
	protected override void AddExtraSelfHarmArgs(LocString description)
	{
		SquKeywords.AddNestedLoc(description, "SummonText", CompanionSummonKeywords.NamedCardText(CompanionId, DynamicVars.Summon));
		description.Add("CompanionName", CompanionSummons.GetName(CompanionId).GetFormattedText());
	}
	protected override void OnUpgrade()
	{
		DynamicVars["SelfHarm"].UpgradeValueBy(3);
		DynamicVars.Summon.UpgradeValueBy(3);
		DynamicVars["CompanionDamage"].UpgradeValueBy(3);
	}
}
