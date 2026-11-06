#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

/// <summary>
/// 不可饮酒致醉：每层让能力触发时多抽一张牌。触发过的同一玩家回合内
/// 再次打出酒时，在酒效果结算前移除整个能力；新普通/额外回合只重置触发标记。
/// </summary>
[RegisterPower]
public sealed class CannotGetDrunkPower : ModPowerTemplate
{
	private sealed class Data
	{
		public bool TriggeredThisTurn { get; set; }
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/TroubleAgainPower.png",
		BigIconPath: "res://images/powers/TroubleAgainPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromCard<Wine>(),
		HoverTipFactory.FromCard<MegaCrit.Sts2.Core.Models.Cards.Dazed>(),
	];

	public override LocString Description
	{
		get
		{
			LocString description = base.Description;
			description.Add("Amount", Amount);
			return description;
		}
	}

	protected override object InitInternalData() => new Data();

	public override async Task BeforeCardPlayed(CardPlay cardPlay)
	{
		if (cardPlay.Card is not Wine wine || wine.Owner?.Creature != Owner)
		{
			return;
		}

		Data data = GetInternalData<Data>();
		if (data.TriggeredThisTurn)
		{
			// "When played" is before the card's effects, so this wine is
			// unprotected and the entire power (all stacks) is removed.
			await PowerCmd.Remove(this);
			return;
		}

		data.TriggeredThisTurn = true;
	}

	public override Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (side == Owner.Side && participants.Contains(Owner))
		{
			GetInternalData<Data>().TriggeredThisTurn = false;
		}

		return Task.CompletedTask;
	}

	public int GetProtectedDrawCount() => Amount;
}
