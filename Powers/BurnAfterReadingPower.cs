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
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ.Audio;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 阅后即焚：打出带消耗的牌后获得 <see cref="Amount"/> 层火种。
/// 层数叠加时增加每次获得的火种。
/// </summary>
[RegisterPower]
public sealed class BurnAfterReadingPower : ModPowerTemplate
{
	private sealed class Data
	{
		public int TriggersThisTurn;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/BurnAfterReadingPower.png",
		BigIconPath: "res://images/powers/BurnAfterReadingPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
		HoverTipFactory.FromPower<TinderPower>(),
		HoverTipFactory.FromPower<BurningPower>(),
	];

	protected override object InitInternalData() => new Data();

	public override Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (side != Owner.Side || !participants.Contains(Owner))
		{
			return Task.CompletedTask;
		}

		GetInternalData<Data>().TriggersThisTurn = 0;
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (Owner.IsDead
			|| Amount <= 0m
			|| Owner.Player is not { } player
			|| cardPlay.Card.Owner != player
			|| cardPlay.PlayIndex != 0
			|| !cardPlay.Card.Keywords.Contains(CardKeyword.Exhaust)
			|| CombatState is not { } combatState
			|| combatState.CurrentSide != Owner.Side)
		{
			return;
		}

		Data data = GetInternalData<Data>();
		data.TriggersThisTurn++;
		string[] cycle = SquSfx.BurnAfterReadingTriggerEvents;
		SquSfx.Play(cycle[(data.TriggersThisTurn - 1) % cycle.Length]);
		Flash();
		await PowerCmd.Apply<TinderPower>(
			choiceContext,
			Owner,
			Amount,
			Owner,
			cardPlay.Card);
	}
}
