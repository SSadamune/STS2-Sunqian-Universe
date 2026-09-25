#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Cards;

/// <summary>咱家不怕酸：攻击并暂时无视易伤、虚弱与脆弱的数值影响。</summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "not_afraid_of_acid")]
public sealed class NotAfraidOfAcid : SlightRevisionCardTemplate<SaidNotAfraidOfAcid>
{
	public const int BaseDamage = 8;
	public const int UpgradedDamage = 11;
	public const int BaseDuration = 2;
	public const int UpgradedDuration = 3;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
		new PowerVar<NotAfraidOfAcidPower>(BaseDuration),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..base.AdditionalHoverTips,
		HoverTipFactory.FromPower<NotAfraidOfAcidPower>(),
		HoverTipFactory.FromPower<VulnerablePower>(),
		HoverTipFactory.FromPower<WeakPower>(),
		HoverTipFactory.FromPower<FrailPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/NotAfraidOfAcid.png");

	public NotAfraidOfAcid()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
		SquSfx.Play(SquSfx.NotAfraidOfAcidEvent);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);

		await PowerCmd.Apply<NotAfraidOfAcidPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(NotAfraidOfAcidPower)].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - BaseDamage);
		DynamicVars[nameof(NotAfraidOfAcidPower)]
			.UpgradeValueBy(UpgradedDuration - BaseDuration);
	}
}
