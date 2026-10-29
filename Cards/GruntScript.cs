using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "grunt_script")]
[RegisterCharacterStarterCard(typeof(SunqianCharacter), 1)]
public sealed class GruntScript : ScriptCardTemplate<GruntScriptRecord>
{
	protected override bool UsesRowKeywordTemplate => false;

	public const int DamageAmount = 8;
	public const int UpgradedDamageAmount = 11;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(DamageAmount, ValueProp.Move),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/GruntScript.png");

	public GruntScript()
		: base(2, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy, true)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
		Creature owner = Owner.Creature;
		int vigorBefore = SquVigorSnapshot.GetAmount(owner);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_blunt")
			.Execute(choiceContext);

		int spentVigor = Math.Max(0, vigorBefore - SquVigorSnapshot.GetAmount(owner));
		if (spentVigor > 0)
		{
			await PowerCmd.Apply<VigorPower>(
				choiceContext,
				owner,
				spentVigor,
				owner,
				this);
			if (owner.GetPower<VigorPower>() is { } restoredVigor)
			{
				AttackVigorResolution.ClearVigorAttackBinding(restoredVigor);
			}
		}

		await UnlockScriptAsync(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamageAmount - DamageAmount);
	}
}
