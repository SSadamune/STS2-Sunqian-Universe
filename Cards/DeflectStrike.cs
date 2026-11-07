using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Character;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Combat.CardTargeting;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 偏折打击：对敌人造成伤害，或为任意存活玩家（包括自己/队友）提供格挡。
/// 自动打出没有目标时按攻击牌惯例选择随机敌人，不会把玩家误当敌人。
/// </summary>
[RegisterCard(typeof(LiubeiCardPool), StableEntryStem = "deflect_strike")]
public sealed class DeflectStrike : ModCardTemplate
{
	public const int BaseDamage = 9;
	public const int UpgradedDamage = 13;
	public const int BaseBlock = 7;
	public const int UpgradedBlock = 10;

	private const string TargetKindVarName = "TargetKind";

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(BaseDamage, ValueProp.Move),
		new TargetBlockVar(BaseBlock),
		ModCardVars.Computed(TargetKindVarName, 2,
			(CardModel? _, Creature? target) =>
				target == null ? 2 : target.IsEnemy ? 1 : 0),
	];

	protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

	public override bool GainsBlock => true;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.Static(StaticHoverTip.Block),
		HoverTipFactory.FromKeyword(SquKeywords.VariousStrikeCards),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/prisoner/DeflectStrike.png");

	public DeflectStrike()
		: base(1, CardType.Attack, CardRarity.Common, CustomTargetType.Anyone)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		Creature? target = cardPlay.Target;
		// Only a genuinely missing target gets the Attack fallback. An explicit
		// auto-play target (enemy or player) must keep its actual branch.
		if (target == null && CombatState is { } combatState)
		{
			target = Owner.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies);
		}

		if (target is not { IsAlive: true })
		{
			return;
		}

		if (target.IsEnemy)
		{
			await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
				.FromCard(this, cardPlay)
				.Targeting(target)
				.WithHitFx("vfx/vfx_attack_slash")
				.Execute(choiceContext);
			return;
		}

		await CreatureCmd.GainBlock(target, DynamicVars.Block, cardPlay);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - BaseDamage);
		DynamicVars.Block.UpgradeValueBy(UpgradedBlock - BaseBlock);
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		int kind = (int)DynamicVars[TargetKindVarName].PreviewValue;
		string suffix = kind switch
		{
			1 => ".enemyBody",
			0 => ".playerBody",
			_ => ".bothBody",
		};
		var body = new LocString("cards", Id.Entry + suffix);
		body.Add(DynamicVars.Damage);
		body.Add(DynamicVars.Block);
		description.Add("BodyText", body);
	}

	private sealed class TargetBlockVar : BlockVar
	{
		public TargetBlockVar(decimal block)
			: base(block, ValueProp.Move)
		{
		}

		public override void UpdateCardPreview(
			CardModel card,
			CardPreviewMode previewMode,
			Creature? target,
			bool runGlobalHooks)
		{
			decimal amount = BaseValue;
			if (card.Enchantment is { } enchantment)
			{
				amount += enchantment.EnchantBlockAdditive(amount);
				amount *= enchantment.EnchantBlockMultiplicative(amount);
				if (!card.IsEnchantmentPreview)
				{
					EnchantedValue = amount;
				}
			}

			if (runGlobalHooks && card.CombatState is { } combatState)
			{
				amount = Hook.ModifyBlock(
					combatState,
					target ?? card.Owner.Creature,
					BaseValue,
					Props,
					card,
					null,
					out _);
			}

			PreviewValue = amount;
		}
	}
}
