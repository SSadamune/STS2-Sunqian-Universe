using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 水火无敌：伤害结算后按实际造成伤害施加灼烧，再按实际施加的灼烧获得格挡。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "water_fire_invincible")]
public sealed class WaterFireInvincible : ModCardTemplate
{
	public const decimal BaseDamage = 5m;

	public const decimal UpgradedDamage = 8m;

	public override bool GainsBlock => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<BurningPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/WaterFireInvincible.png");

	public WaterFireInvincible()
		: base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		AttackCommand attackCommand = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);

		int damageDealt = attackCommand.Results
			.SelectMany(results => results)
			.Sum(result => result.TotalDamage);
		if (damageDealt <= 0)
		{
			return;
		}

		Creature target = cardPlay.Target;
		int burningBefore = target.GetPowerAmount<BurningPower>();

		await PowerCmd.Apply<BurningPower>(
			choiceContext,
			target,
			damageDealt,
			Owner.Creature,
			this);

		int burningApplied = target.GetPowerAmount<BurningPower>() - burningBefore;
		if (burningApplied <= 0)
		{
			return;
		}

		await CreatureCmd.GainBlock(Owner.Creature, burningApplied, ValueProp.Move, cardPlay);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - BaseDamage);
	}
}
