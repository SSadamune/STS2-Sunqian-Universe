using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 掘地突袭：无视格挡伤害并给予灼烧。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "dig_raid")]
public sealed class DigRaid : ModCardTemplate
{
	public const decimal CanonicalDamage = 11m;
	public const decimal UpgradedDamage = 14m;
	public const decimal CanonicalBurning = 6m;
	public const decimal UpgradedBurning = 8m;

	private static readonly ValueProp DamageProps = ValueProp.Move | ValueProp.Unblockable;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(CanonicalDamage, DamageProps),
		new PowerVar<BurningPower>(CanonicalBurning),
	];

	protected override HashSet<CardTag> CanonicalTags => [SquCardTags.Burning];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.Static(StaticHoverTip.Block),
		HoverTipFactory.FromPower<BurningPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/DigRaid.png");

	public DigRaid()
		: base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.WithDamageProps(DamageProps)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);

		await PowerCmd.Apply<BurningPower>(
			choiceContext,
			cardPlay.Target,
			DynamicVars[nameof(BurningPower)].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - CanonicalDamage);
		DynamicVars[nameof(BurningPower)].UpgradeValueBy(UpgradedBurning - CanonicalBurning);
	}
}
