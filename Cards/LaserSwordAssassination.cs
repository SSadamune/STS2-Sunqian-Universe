using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 激光剑行刺：升级前造成无视格挡的伤害，升级后造成穿透伤害；
/// 若目标意图不是攻击，则在力量、活力等加成之后将伤害翻倍。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "laser_sword_assassination")]
public sealed class LaserSwordAssassination : ModCardTemplate, IPenetratingDamageCard
{
	public const int CanonicalDamage = 4;
	public const int DamageMultiplier = 3;
	private const string IsAmplifiedVarName = "IsAmplified";

	internal static readonly ValueProp DamageProps = ValueProp.Move | ValueProp.Unblockable;

	public bool DealsPenetratingDamage => IsUpgraded;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(CanonicalDamage, DamageProps),
		ModCardVars.Computed(
			IsAmplifiedVarName,
			0m,
			(CardModel? card, Creature? target) => ShouldAmplifyDamage(target) ? 1m : 0m),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips
	{
		get
		{
			yield return HoverTipFactory.Static(StaticHoverTip.Block);
			if (IsUpgraded)
			{
				yield return HoverTipFactory.FromKeyword(SquKeywords.PiercingDamage);
			}
		}
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/LaserSwordAssassination.png");

	protected override bool ShouldGlowGoldInternal
	{
		get
		{
			ICombatState? combatState = CombatState;
			if (combatState == null)
			{
				return false;
			}

			foreach (Creature enemy in combatState.HittableEnemies)
			{
				if (enemy.IsAlive && !SquEnemyIntent.IntendsToAttack(enemy))
				{
					return true;
				}
			}

			return false;
		}
	}

	public LaserSwordAssassination()
		: base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		SquSfx.Play(ShouldAmplifyDamage(cardPlay.Target)
			? SquSfx.LaserSwordAssassinationLaserEvent
			: SquSfx.LaserSwordAssassinationDrawEvent);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.WithDamageProps(DamageProps)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		string bodyKey = DynamicVars[IsAmplifiedVarName].PreviewValue > 0m
			? Id.Entry + ".amplifiedBody"
			: Id.Entry + ".normalBody";
		var body = new LocString("cards", bodyKey);
		body.Add(DynamicVars.Damage);
		body.Add(new IfUpgradedVar(
			IsUpgraded ? UpgradeDisplay.Upgraded : UpgradeDisplay.Normal));
		description.Add("BodyText", body);
	}

	internal static bool ShouldAmplifyDamage(Creature? target) =>
		target is { IsAlive: true } && !SquEnemyIntent.IntendsToAttack(target);

	/// <summary>
	/// 让固定三倍倍率参与 Hook.ModifyDamage 的乘算阶段；升级只改变是否穿透防御能力。
	/// </summary>
	public override decimal ModifyDamageMultiplicative(
		Creature? target,
		decimal amount,
		ValueProp props,
		Creature? dealer,
		CardModel? cardSource,
		CardPlay? cardPlay)
	{
		if (cardSource != this
			|| !props.IsPoweredAttack()
			|| !ShouldAmplifyDamage(target))
		{
			return 1m;
		}

		return DamageMultiplier;
	}
}
