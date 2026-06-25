using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class SunqianUniversePower : ModPowerTemplate
{
	private bool _drewFromScriptLiftThisTurn;

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

	public override Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (side != Owner.Side || !participants.Contains(Owner))
		{
			return Task.CompletedTask;
		}

		_drewFromScriptLiftThisTurn = false;
		return Task.CompletedTask;
	}

	public async Task OnScriptLiftedAsync(PlayerChoiceContext choiceContext)
	{
		if (_drewFromScriptLiftThisTurn)
		{
			return;
		}

		int drawCount = Amount;
		Player? player = Owner.Player;
		if (drawCount <= 0 || player is null)
		{
			return;
		}

		_drewFromScriptLiftThisTurn = true;
		Flash();
		await CardPileCmd.Draw(choiceContext, drawCount, player);
	}
}
