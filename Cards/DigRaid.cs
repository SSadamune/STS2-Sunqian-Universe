using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 掘地突袭：无视格挡伤害并给予灼烧。灼烧额外吃活力；升级后伤害与灼烧同时吃活力和火种。
/// 卡面预览把加成写进 PreviewValue，印面 BaseValue 不变以便 :diff() 标绿。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "dig_raid")]
public sealed class DigRaid : ModCardTemplate
{
	public const int CanonicalDamage = 8;
	public const int UpgradedDamage = 11;
	public const int CanonicalBurning = 5;
	public const int UpgradedBurning = 7;

	private static readonly ValueProp DamageProps = ValueProp.Move | ValueProp.Unblockable;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new TinderBoostedDamageVar(CanonicalDamage),
		new VigorBoostedBurningVar(CanonicalBurning),
	];

	protected override HashSet<CardTag> CanonicalTags => [SquCardTags.Burning];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.Static(StaticHoverTip.Block),
		HoverTipFactory.FromPower<BurningPower>(),
		HoverTipFactory.FromPower<VigorPower>(),
		HoverTipFactory.FromPower<TinderPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/DigRaid.png");

	protected override bool ShouldGlowGoldInternal
	{
		get
		{
			if (Owner?.Creature is not Creature owner)
			{
				return false;
			}

			return SquVigorSnapshot.GetAmount(owner) > 0
				|| (IsUpgraded && GetTinderAmount(owner) > 0);
		}
	}

	public DigRaid()
		: base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		SquSfx.Play(SquSfx.DigRaidEvent);
		Creature owner = Owner.Creature;
		int vigor = SquVigorSnapshot.GetEffectiveAmount(owner, this);
		decimal tinderBonus = IsUpgraded ? GetTinderAmount(owner) : 0m;

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue + tinderBonus)
			.WithDamageProps(DamageProps)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);

		await PowerCmd.Apply<BurningPower>(
			choiceContext,
			cardPlay.Target,
			DynamicVars[nameof(BurningPower)].BaseValue + vigor,
			owner,
			this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(UpgradedDamage - CanonicalDamage);
		DynamicVars[nameof(BurningPower)].UpgradeValueBy(UpgradedBurning - CanonicalBurning);
	}

	internal static decimal GetTinderAmount(Creature? creature) =>
		creature?.GetPower<TinderPower>() is { Amount: > 0 } tinder ? tinder.Amount : 0m;

	/// <summary>
	/// 升级后伤害预览包含当前火种；BaseValue 保持印面数值以便标绿。
	/// </summary>
	private sealed class TinderBoostedDamageVar : DamageVar
	{
		public TinderBoostedDamageVar(decimal baseValue)
			: base(baseValue, DamageProps)
		{
		}

		public override void UpdateCardPreview(
			CardModel card,
			CardPreviewMode previewMode,
			Creature? target,
			bool runGlobalHooks)
		{
			decimal amount = BaseValue;
			if (card.IsUpgraded && card.Pile?.Type is PileType.Hand or PileType.Play)
			{
				amount += GetTinderAmount(card.Owner?.Creature);
			}

			if (!runGlobalHooks
				|| card.Owner is not { Creature: { } creature } owner
				|| card.CombatState is not { } combatState)
			{
				PreviewValue = amount;
				return;
			}

			PreviewValue = Math.Max(
				0m,
				Hook.ModifyDamage(
					owner.RunState,
					combatState,
					target,
					creature,
					amount,
					Props,
					card,
					null,
					ModifyDamageHookType.All,
					previewMode,
					out _));
		}
	}

	/// <summary>
	/// 手牌灼烧预览包含当前活力，再走火种/好火等给予层数修正；BaseValue 保持印面数值以便标绿。
	/// 抽牌堆/弃牌堆不显示活力加成。
	/// </summary>
	private sealed class VigorBoostedBurningVar : PowerVar<BurningPower>
	{
		public VigorBoostedBurningVar(decimal baseValue)
			: base(baseValue)
		{
		}

		public override void UpdateCardPreview(
			CardModel card,
			CardPreviewMode previewMode,
			Creature? target,
			bool runGlobalHooks)
		{
			if (card.Owner?.Creature is not Creature owner)
			{
				PreviewValue = BaseValue;
				return;
			}

			decimal amount = BaseValue + SquVigorSnapshot.GetAmountForCardPreview(card);
			if (!runGlobalHooks || card.CombatState is not { } combatState)
			{
				PreviewValue = amount;
				return;
			}

			PreviewValue = Hook.ModifyPowerAmountGiven(
				combatState,
				ModelDb.Power<BurningPower>(),
				owner,
				amount,
				target,
				card,
				out IEnumerable<AbstractModel> _);
		}
	}
}
