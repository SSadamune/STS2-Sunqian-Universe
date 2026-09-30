#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

/// <summary>
/// 酸蚀抗性：保留易伤、虚弱与脆弱本身及其正常获得/倒计时，只抵消它们的数值倍率。
/// </summary>
[RegisterPower]
public sealed class AcidEtchResistancePower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/AcidEtchResistancePower.png",
		BigIconPath: "res://images/powers/AcidEtchResistancePowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VulnerablePower>(),
		HoverTipFactory.FromPower<WeakPower>(),
		HoverTipFactory.FromPower<FrailPower>(),
	];

	public override async Task AfterSideTurnEnd(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IEnumerable<Creature> participants)
	{
		if (side != Owner.Side || !participants.Contains(Owner) || Amount <= 0)
		{
			return;
		}

		await PowerCmd.TickDownDuration(this);
	}
}
