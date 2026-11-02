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
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "awaken_second_brother")]
[RegisterCharacterStarterCard(typeof(LiubeiCharacter), 1)]
public sealed class AwakenSecondBrother : SelfHarmCardTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("SelfHarm", 8), new SummonVar(8),
		new CompanionDamageVar(CompanionSummons.SecondBrotherId, 8),
	];
	public override CardAssetProfile AssetProfile => new(PortraitPath: "res://images/cards/GuanDiForm.png");
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..(SelfHarmSystem.HasTriggered(this) ? Array.Empty<IHoverTip>() :
			new[] { SquKeywords.CreateSelfHarmHoverTip(SelfHarmSystem.GetAmountVar(this)) }),
		CompanionSummonKeywords.NamedHoverTip(CompanionSummons.SecondBrotherId, DynamicVars.Summon),
		HoverTipFactory.FromPower<LifeContractPower>(),
	];
	public AwakenSecondBrother() : base(2, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy) { }
	protected override Task AfterSelfHarmPaid(PlayerChoiceContext context, int amount)
	{
		LifeContractSystem.AddContribution(Owner, amount);
		return Task.CompletedTask;
	}
	protected override async Task PlayAfterSelfHarm(PlayerChoiceContext context, CardPlay play)
	{
		ArgumentNullException.ThrowIfNull(play.Target);
		var summoned = await CompanionSummons.SummonSpecific(context, Owner,
			CompanionSummons.SecondBrotherId, DynamicVars.Summon.BaseValue, this);
		if (summoned.Creature is not { IsAlive: true } pet) return;
		await DamageCmd.Attack(DynamicVars["CompanionDamage"].BaseValue)
			.FromCompanion(pet, this, play).Targeting(play.Target)
			.WithHitFx("vfx/vfx_attack_blunt", tmpSfx: "blunt_attack.mp3").Execute(context);
	}
	protected override void AddExtraSelfHarmArgs(LocString description) =>
		SquKeywords.AddNestedLoc(description, "SummonText",
			CompanionSummonKeywords.NamedCardText(CompanionSummons.SecondBrotherId, DynamicVars.Summon));
	protected override void OnUpgrade()
	{
		DynamicVars["SelfHarm"].UpgradeValueBy(3);
		DynamicVars.Summon.UpgradeValueBy(3);
		DynamicVars["CompanionDamage"].UpgradeValueBy(3);
	}
}
