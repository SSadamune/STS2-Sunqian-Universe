#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Squ.Powers;

/// <summary>
/// 英雄相惜：本回合每次本人实际打出攻击牌，在卡牌效果前对每个存活敌人施加
/// 等同于层数的虚弱；玩家回合结束（包括进入额外回合）移除全部层数。
/// </summary>
[RegisterPower]
public sealed class HeroesSympathyPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/TroubleAgainPower.png",
		BigIconPath: "res://images/powers/TroubleAgainPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<WeakPower>(),
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

	public override async Task BeforeCardPlayed(CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner?.Creature != Owner || cardPlay.Card.Type != CardType.Attack)
		{
			return;
		}

		ICombatState? combatState = CombatState;
		if (combatState == null)
		{
			return;
		}

		var context = new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext();
		foreach (Creature enemy in combatState.Enemies.Where(enemy => enemy.IsAlive).ToArray())
		{
			await PowerCmd.Apply<WeakPower>(
				context,
				enemy,
				Amount,
				Owner,
				cardPlay.Card);
		}
	}

	public override async Task AfterSideTurnEnd(
		MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext choiceContext,
		CombatSide side,
		IEnumerable<Creature> participants)
	{
		if (side == Owner.Side && participants.Contains(Owner))
		{
			await PowerCmd.Remove(this);
		}
	}
}
