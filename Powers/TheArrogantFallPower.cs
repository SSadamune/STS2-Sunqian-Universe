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

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 「骄兵必败」延迟 debuff：在下个回合开始时施加等量层数的虚弱与易伤；层数可叠加。
/// </summary>
[RegisterPower]
public sealed class TheArrogantFallPower : ModPowerTemplate
{
	public const int DebuffAmountPerStack = 3;

	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/TheArrogantFallPower.png",
		BigIconPath: "res://images/powers/TheArrogantFallPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<WeakPower>(),
		HoverTipFactory.FromPower<VulnerablePower>(),
	];

	public override async Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (!participants.Contains(Owner) || Owner.IsDead || Amount <= 0 || AmountOnTurnStart == 0)
		{
			return;
		}

		Flash();

		PlayerChoiceContext choiceContext = new ThrowingPlayerChoiceContext();
		await PowerCmd.Apply<WeakPower>(choiceContext, Owner, Amount, Applier, null);
		await PowerCmd.Apply<VulnerablePower>(choiceContext, Owner, Amount, Applier, null);
		await PowerCmd.Remove(this);
	}
}
