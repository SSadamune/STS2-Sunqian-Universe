using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 关帝形态：持有者打出牌后，按该牌消耗的能量获得格挡与活力（Amount = 每点能量收益，可叠加）。
/// </summary>
[RegisterPower]
public sealed class GuanDiFormPower : ModPowerTemplate
{
	private static readonly ValueProp BlockProps = ValueProp.Move;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/GuanDiFormPower.png",
		BigIconPath: "res://images/powers/GuanDiFormPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(2, BlockProps),
		new PowerVar<VigorPower>(2),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.Static(StaticHoverTip.Block),
		HoverTipFactory.FromPower<VigorPower>(),
	];

	public override Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (power == this)
		{
			SyncDynamicVarsFromAmount();
		}

		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (Owner.IsDead || cardPlay.Card.Owner != Owner.Player || cardPlay.PlayIndex != 0)
		{
			return;
		}

		int energySpent = cardPlay.Resources.EnergySpent;
		if (energySpent <= 0 || Amount <= 0m)
		{
			return;
		}

		decimal gain = Amount * energySpent;
		Flash();
		await CreatureCmd.GainBlock(Owner, gain, BlockProps, cardPlay: null);
		await PowerCmd.Apply<VigorPower>(choiceContext, Owner, gain, Owner, cardPlay.Card);
	}

	private void SyncDynamicVarsFromAmount()
	{
		DynamicVars.Block.BaseValue = Amount;
		DynamicVars[nameof(VigorPower)].BaseValue = Amount;
	}
}
