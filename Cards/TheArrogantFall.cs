using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Powers;
using STS2RitsuLib.Combat.CardTargeting;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(ColorlessCardPool), StableEntryStem = "the_arrogant_fall")]
public sealed class TheArrogantFall : ModCardTemplate
{
	private const decimal VigorAmount = 7m;
	private const decimal BlockAmount = 7m;

	private static readonly ValueProp BlockProps = ValueProp.Move;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VigorPower>(VigorAmount),
		new TargetBlockVar(BlockAmount, BlockProps),
		new PowerVar<WeakPower>(TheArrogantFallPower.DebuffAmountPerStack),
		new PowerVar<VulnerablePower>(TheArrogantFallPower.DebuffAmountPerStack),
	];

	public override bool GainsBlock => true;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>(),
		HoverTipFactory.Static(StaticHoverTip.Block),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/TheArrogantFall.png");

	public TheArrogantFall()
		: base(0, CardType.Power, CardRarity.Common, CustomTargetType.Anyone)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

		await PowerCmd.Apply<VigorPower>(
			choiceContext,
			cardPlay.Target,
			DynamicVars[nameof(VigorPower)].BaseValue,
			Owner.Creature,
			this);

		await CreatureCmd.GainBlock(cardPlay.Target, DynamicVars.Block, cardPlay: null);

		await PowerCmd.Apply<TheArrogantFallPower>(
			choiceContext,
			cardPlay.Target,
			TheArrogantFallPower.DebuffAmountPerStack,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Retain);
	}

	/// <summary>
	/// 格挡按获得格挡的目标计算（含敏捷）；无目标预览时不套用出牌者敏捷。
	/// </summary>
	private sealed class TargetBlockVar : BlockVar
	{
		public TargetBlockVar(decimal block, ValueProp props)
			: base(block, props)
		{
		}

		public override void UpdateCardPreview(
			CardModel card,
			CardPreviewMode previewMode,
			Creature? target,
			bool runGlobalHooks)
		{
			decimal amount = BaseValue;

			EnchantmentModel? enchantment = card.Enchantment;
			if (enchantment != null)
			{
				amount += enchantment.EnchantBlockAdditive(amount);
				amount *= enchantment.EnchantBlockMultiplicative(amount);
				if (!card.IsEnchantmentPreview)
				{
					EnchantedValue = amount;
				}
			}

			if (!runGlobalHooks || target == null || card.CombatState == null)
			{
				PreviewValue = Math.Max(amount, 0m);
				return;
			}

			amount = Hook.ModifyBlock(
				card.CombatState,
				target,
				amount,
				Props,
				cardSource: null,
				cardPlay: null,
				out _);

			PreviewValue = Math.Max(amount, 0m);
		}
	}
}
