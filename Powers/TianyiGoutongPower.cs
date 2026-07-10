using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 「天意沟通」的持续能力：每回合开始时[gold]预见[/gold] <see cref="Amount"/>，
/// 因此置入弃牌堆的牌改为[gold]消耗[/gold]，若为[gold]攻击[/gold]/[gold]技能[/gold]牌则
/// 分别获得1点[gold]力量[/gold]/[gold]敏捷[/gold]。
/// </summary>
[RegisterPower]
public sealed class TianyiGoutongPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/TianyiGoutongPower.png",
		BigIconPath: "res://images/powers/TianyiGoutongPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.Scry),
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<DexterityPower>(),
	];

	public override async Task BeforeSideTurnStart(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (!participants.Contains(Owner) || Owner.IsDead || Owner.Player is not { } player)
		{
			return;
		}

		Flash();
		await ExecuteProcess(choiceContext, player, (int)Amount);
	}

	/// <summary>
	/// 执行一次「预见→改为消耗→按牌型加成」的流程，供本能力的回合开始触发与
	/// <see cref="Squ.Cards.TianyiGoutong"/> 的打出效果共用。
	/// </summary>
	public static Task<ScryResult> ExecuteProcess(
		PlayerChoiceContext choiceContext,
		Player player,
		int scryAmount)
	{
		return ScryCmd.Execute(choiceContext, player, scryAmount, HandleChosenCard);
	}

	private static async Task HandleChosenCard(PlayerChoiceContext choiceContext, CardModel card)
	{
		Creature owner = card.Owner.Creature;

		await CardCmd.Exhaust(choiceContext, card);

		switch (card.Type)
		{
			case CardType.Attack:
				await PowerCmd.Apply<StrengthPower>(choiceContext, owner, 1m, owner, null);
				break;
			case CardType.Skill:
				await PowerCmd.Apply<DexterityPower>(choiceContext, owner, 1m, owner, null);
				break;
		}
	}
}
