using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class GoodBrotherTangXiaohuPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/GoodBrotherTangXiaohuPower.png",
		BigIconPath: "res://images/powers/GoodBrotherTangXiaohuPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new OwnerBlockVar(5m, ValueProp.Move),
	];

	public new IEnumerable<IHoverTip> HoverTips
	{
		get
		{
			RefreshBlockPreview();
			return base.HoverTips;
		}
	}

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.Script),
		HoverTipFactory.Static(StaticHoverTip.Block),
	];

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		DynamicVars.Block.BaseValue = Amount;
		RefreshBlockPreview();
		return Task.CompletedTask;
	}

	public override Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (power != this)
		{
			return Task.CompletedTask;
		}

		DynamicVars.Block.BaseValue = Amount;
		RefreshBlockPreview();
		return Task.CompletedTask;
	}

	private void RefreshBlockPreview()
	{
		if (DynamicVars.Block is OwnerBlockVar ownerBlock)
		{
			ownerBlock.RefreshPreview(Owner, CombatState);
		}
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (Owner.IsDead || cardPlay.Card.Owner != Owner.Player
			|| !cardPlay.Card.Tags.Contains(SquCardTags.Script))
		{
			return;
		}

		Flash();
		await CreatureCmd.GainBlock(Owner, DynamicVars.Block, cardPlay: null);
	}

	/// <summary>
	/// 能力悬停不走 CardModel.UpdateDynamicVarPreview，需自行用 Hook.ModifyBlock 刷新敏捷等修正后的预览。
	/// </summary>
	private sealed class OwnerBlockVar : BlockVar
	{
		public OwnerBlockVar(decimal block, ValueProp props)
			: base(block, props)
		{
		}

		public void RefreshPreview(Creature owner, ICombatState? combatState)
		{
			decimal amount = BaseValue;
			if (combatState != null)
			{
				amount = Hook.ModifyBlock(
					combatState,
					owner,
					amount,
					Props,
					cardSource: null,
					cardPlay: null,
					out _);
			}

			PreviewValue = Math.Max(amount, 0m);
		}
	}
}
