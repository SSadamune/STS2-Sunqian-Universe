using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 「多智近妖」：与原版 <see cref="MegaCrit.Sts2.Core.Models.Powers.PaleBlueDotPower"/> 相同，
/// 在抽牌阶段回溯上一玩家回合的战斗历史，故本回合在打出此能力前打出的消耗牌同样计入。
/// </summary>
[RegisterPower]
public sealed class SupernaturallyWisePower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/SupernaturallyWisePower.png",
		BigIconPath: "res://images/powers/SupernaturallyWisePowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
	];

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		if (player != Owner.Player || !PlayedExhaustCardLastTurn(player))
		{
			return count;
		}

		return count + Amount;
	}

	public override Task AfterModifyingHandDraw()
	{
		Flash();
		return Task.CompletedTask;
	}

	private static bool PlayedExhaustCardLastTurn(Player player) =>
		CombatManager.Instance.History.CardPlaysFinished.Any(entry =>
			entry.HappenedLastPlayerTurn(player)
			&& entry.CardPlay.Card.Owner == player
			&& entry.CardPlay.Card.Keywords.Contains(CardKeyword.Exhaust));
}
