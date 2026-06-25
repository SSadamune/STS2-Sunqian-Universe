using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class SunqianUniversePower : ModPowerTemplate, IScriptLiftHandler
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/SunqianUniversePower.png",
		BigIconPath: "res://images/powers/SunqianUniversePowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.Script),
	];

	public async Task OnScriptLiftAsync(ScriptLiftContext context)
	{
		if (!context.IsFirstLiftOfTurn)
		{
			return;
		}

		int drawCount = Amount;
		Player? player = Owner.Player;
		if (drawCount <= 0 || player is null)
		{
			return;
		}

		Flash();
		await CardPileCmd.Draw(context.ChoiceContext, drawCount, player);
	}
}
