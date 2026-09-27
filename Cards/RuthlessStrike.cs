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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 无情打击：无视格挡伤害。蓄能：手牌中每有一张己方牌被消耗，额外造成 1 次伤害。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "ruthless_strike")]
public sealed class RuthlessStrike : ChargeCardTemplate
{
	public const int CanonicalDamage = 9;
	public const int BaseHits = 1;
	public const int UpgradedHits = 2;

	private static readonly ValueProp DamageProps = ValueProp.Move | ValueProp.Unblockable;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(CanonicalDamage, DamageProps),
		new ChargedRepeatVar(
			BaseHits,
			card => GetCanonicalHits(card)),
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
		Clear: ResetHitsUntilPlayed);

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/RuthlessStrike.png");

	protected override bool ShouldGlowGoldInternal => HasChargedHits;

	public RuthlessStrike()
		: base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		if (HasChargedHits)
		{
			SquSfx.Play(SquSfx.RuthlessStrikeSwordSoundEvent);
		}

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
		DynamicVars.Repeat.UpgradeValueBy(UpgradedHits - BaseHits);
	}

	public override Task AfterCardExhausted(
		PlayerChoiceContext choiceContext,
		CardModel card,
		bool causedByEthereal)
	{
		if (Pile?.Type != PileType.Hand || card.Owner != Owner)
		{
			return Task.CompletedTask;
		}

		DynamicVars.Repeat.BaseValue += 1m;
		RefreshCardVisuals();
		SquSfx.Play(SquSfx.RuthlessStrikeDontForceMeEvent);
		return Task.CompletedTask;
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
		// 图鉴与战斗外预览使用印面描述；仅战斗中实际蓄能后显示动态攻击次数。
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

	private void ResetHitsUntilPlayed()
	{
		DynamicVars.Repeat.BaseValue = CanonicalHits;
		RefreshCardVisuals();
	}

	private int CanonicalHits => GetCanonicalHits(this);

	private static int GetCanonicalHits(CardModel card) =>
		card.IsUpgraded ? UpgradedHits : BaseHits;
}
