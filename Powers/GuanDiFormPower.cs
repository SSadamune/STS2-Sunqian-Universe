using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 关帝形态：持有者整张牌结算后，按该牌实际花费的能量获得不受敏捷影响的格挡与活力。
/// </summary>
[RegisterPower]
public sealed class GuanDiFormPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/GuanDiFormPower.png",
		BigIconPath: "res://images/powers/GuanDiFormPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.Static(StaticHoverTip.Block),
		HoverTipFactory.FromPower<VigorPower>(),
	];

	public override async Task AfterCardPlayedLate(
		PlayerChoiceContext choiceContext,
		CardPlay cardPlay)
	{
		if (Amount <= 0m
			|| cardPlay.Card.Owner.Creature != Owner
			|| cardPlay.PlayIndex != cardPlay.PlayCount - 1)
		{
			return;
		}

		int energySpent = cardPlay.Resources.EnergySpent;
		if (energySpent <= 0)
		{
			return;
		}

		decimal gain = Amount * energySpent;
		Flash();
		await CreatureCmd.GainBlock(Owner, gain, ValueProp.Unpowered, cardPlay: null);
		await PowerCmd.Apply<VigorPower>(
			choiceContext,
			Owner,
			gain,
			Owner,
			cardPlay.Card);
	}
}
