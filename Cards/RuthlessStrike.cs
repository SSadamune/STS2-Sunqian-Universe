using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 无情打击：无视格挡伤害。蓄能：手牌中每打出一张消耗牌，额外造成 1 次伤害。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "ruthless_strike")]
public sealed class RuthlessStrike : ChargeCardTemplate
{
	public const decimal CanonicalDamage = 7m;
	public const decimal UpgradedDamage = 9m;
	public const int CanonicalHits = 1;

	private static readonly ValueProp DamageProps = ValueProp.Move | ValueProp.Unblockable;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(CanonicalDamage, DamageProps),
		new RepeatVar(CanonicalHits),
	];

	protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.Static(StaticHoverTip.Block),
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
	];

	protected override string ChargeEffectLocKey => Id.Entry + ".chargeEffect";

	protected override ChargeHooks Charge => new(
		OnRetained: null,
		OnPowerAmountChanged: null,
		Clear: ResetHitsUntilPlayed,
		OnCardPlayed: GainExtraHitFromExhaustCard);

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/RuthlessStrike.png");

	protected override bool ShouldGlowGoldInternal => HasChargedHits;

	public RuthlessStrike()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.WithDamageProps(DamageProps)
			.WithHitCount(DynamicVars.Repeat.IntValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - CanonicalDamage);
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		LocString body;
		if (ShouldShowChargedDescription())
		{
			body = new LocString("cards", Id.Entry + ".chargedDescription");
			body.Add(DynamicVars.Repeat);
		}
		else
		{
			body = new LocString("cards", Id.Entry + ".normalBody");
		}

		body.Add(DynamicVars.Damage);
		SquKeywords.AddNestedLoc(
			body,
			"ChargeText",
			SquKeywords.FormatChargeCardText(this, ChargeEffectLocKey));
		SquKeywords.AddNestedLoc(description, "BodyText", body);
	}

	private bool ShouldShowChargedDescription()
	{
		// 图鉴规范卡不可变；仅战斗中的可变实例在蓄能已叠加后改写描述。
		if (!IsMutable
			|| RunState is null
			|| !CombatManager.Instance.IsInProgress
			|| Owner?.PlayerCombatState == null)
		{
			return false;
		}

		return HasChargedHits;
	}

	private bool HasChargedHits => DynamicVars.Repeat.IntValue > CanonicalHits;

	private Task GainExtraHitFromExhaustCard(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != Owner
			|| cardPlay.PlayIndex != 0
			|| !cardPlay.Card.Keywords.Contains(CardKeyword.Exhaust))
		{
			return Task.CompletedTask;
		}

		DynamicVars.Repeat.BaseValue += 1m;
		return Task.CompletedTask;
	}

	private void ResetHitsUntilPlayed()
	{
		DynamicVars.Repeat.BaseValue = CanonicalHits;
	}
}
