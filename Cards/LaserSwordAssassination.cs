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
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 激光剑行刺：无视格挡。若目标意图不是攻击，则在力量、活力等加成之后将伤害翻倍。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "laser_sword_assassination")]
public sealed class LaserSwordAssassination : ModCardTemplate
{
	public const decimal CanonicalDamage = 8m;
	public const decimal UpgradedDamage = 11m;

	internal static readonly ValueProp DamageProps = ValueProp.Move | ValueProp.Unblockable;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DoublingDamageVar(),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.Static(StaticHoverTip.Block),
	];

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
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
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
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - CanonicalDamage);
	}

	internal static bool ShouldDoubleDamage(Creature? target) =>
		target is { IsAlive: true } && !SquEnemyIntent.IntendsToAttack(target);

	internal static bool IsUpdatingCardPreview { get; private set; }

	/// <summary>
	/// 指向非攻击意图的敌人时，预览伤害为力量/活力等加成之后再翻倍；
	/// <see cref="DynamicVar.BaseValue"/> 保持印面数值，以便 <c>{Damage:diff()}</c> 标绿。
	/// </summary>
	private sealed class DoublingDamageVar : DamageVar
	{
		public DoublingDamageVar()
			: base(CanonicalDamage, DamageProps)
		{
		}

		public override void UpdateCardPreview(
			CardModel card,
			CardPreviewMode previewMode,
			Creature? target,
			bool runGlobalHooks)
		{
			IsUpdatingCardPreview = true;
			try
			{
				base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
			}
			finally
			{
				IsUpdatingCardPreview = false;
			}

			if (ShouldDoubleDamage(target))
			{
				PreviewValue *= 2m;
			}
		}
	}
}
