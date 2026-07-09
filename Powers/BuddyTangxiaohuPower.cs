using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
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
public sealed class BuddyTangxiaohuPower : ModPowerTemplate
{
	private static readonly ValueProp BlockProps = ValueProp.Move;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/BuddyTangxiaohuPower.png",
		BigIconPath: "res://images/powers/BuddyTangxiaohuPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(4m, BlockProps),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.Script),
		HoverTipFactory.Static(StaticHoverTip.Block),
	];

	internal void RefreshBlockPreviewForHover() => RefreshBlockPreview();

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
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
		if (power != this && power.Owner != Owner)
		{
			return Task.CompletedTask;
		}

		RefreshBlockPreview();
		return Task.CompletedTask;
	}

	private void RefreshBlockPreview()
	{
		decimal baseBlock = Amount;
		DynamicVars.Block.BaseValue = baseBlock;

		decimal preview = baseBlock;
		if (Owner.CombatState != null)
		{
			preview = Hook.ModifyBlock(
				Owner.CombatState,
				Owner,
				baseBlock,
				BlockProps,
				cardSource: null,
				cardPlay: null,
				out _);
		}

		DynamicVars.Block.PreviewValue = Math.Max(preview, 0m);
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
}
