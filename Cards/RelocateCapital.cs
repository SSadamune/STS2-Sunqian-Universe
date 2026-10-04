using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>迁都：对所有敌人造成伤害，并按本牌实际消耗的活力获得格挡；升级后也获得等量火种。</summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "relocate_capital")]
public sealed class RelocateCapital : SlightRevisionCardTemplate<BurnCity>
{
	public const int DamageAmount = 9;
	public const int UpgradedDamageAmount = 12;
	public const int HighVigorThreshold = 10;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(DamageAmount, ValueProp.Move),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips
	{
		get
		{
			foreach (IHoverTip tip in base.AdditionalHoverTips)
			{
				yield return tip;
			}

			yield return HoverTipFactory.FromPower<VigorPower>();
			yield return HoverTipFactory.Static(StaticHoverTip.Block);
			yield return HoverTipFactory.FromPower<TinderPower>();
		}
	}

	public override bool GainsBlock => true;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/RelocateCapital.png");

	protected override bool ShouldGlowGoldInternal =>
		Owner?.Creature is Creature owner && SquVigorSnapshot.GetAmount(owner) > 0;

	public RelocateCapital()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
		Creature owner = Owner.Creature;
		int vigorBefore = SquVigorSnapshot.GetAmount(owner);
		SquSfx.Play(vigorBefore >= HighVigorThreshold
			? SquSfx.RelocateCapitalHighVigorEvent
			: SquSfx.RelocateCapitalEvent);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);

		int vigorSpent = vigorBefore - SquVigorSnapshot.GetAmount(owner);
		if (vigorSpent <= 0)
		{
			return;
		}

		await CreatureCmd.GainBlock(owner, vigorSpent, ValueProp.Move, cardPlay);
		await PowerCmd.Apply<TinderPower>(
			choiceContext,
			owner,
			vigorSpent,
			owner,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamageAmount - DamageAmount);
	}
}
