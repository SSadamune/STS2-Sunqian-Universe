using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
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
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/BuddyTangxiaohuPower.png",
		BigIconPath: "res://images/powers/BuddyTangxiaohuPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.Script),
		HoverTipFactory.Static(StaticHoverTip.Block),
	];

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (Owner.IsDead || Amount <= 0m || cardPlay.Card.Owner != Owner.Player
			|| !cardPlay.Card.Tags.Contains(SquCardTags.Script))
		{
			return;
		}

		Flash();
		await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, cardPlay: null);
	}
}
