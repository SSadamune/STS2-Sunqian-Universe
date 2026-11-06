using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 「欲擒故纵」跳过选择后挂载的延迟效果：在下一个玩家回合、正常抽牌之前，
/// 把仍位于抽牌堆或弃牌堆的候选牌全部放入手牌。每次跳过选择各挂一份，
/// 每份只持有自己那一批牌的实体引用，不按牌名或 Entry 重新匹配。
/// </summary>
[RegisterPower]
public sealed class LureAndReleasePower : ModPowerTemplate
{
	private sealed class Data
	{
		public List<CardModel> Cards { get; } = [];
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

	public override PowerStackType StackType => PowerStackType.None;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/TroubleAgainPower.png",
		BigIconPath: "res://images/powers/TroubleAgainPowerBig.png");

	protected override object InitInternalData() => new Data();

	public static async Task ApplyAsync(
		PlayerChoiceContext choiceContext,
		Player player,
		IReadOnlyList<CardModel> cards,
		CardModel source)
	{
		LureAndReleasePower? power = await PowerCmd.Apply<LureAndReleasePower>(
			choiceContext,
			player.Creature,
			1m,
			player.Creature,
			source);
		power?.SetCards(cards);
	}

	public void SetCards(IEnumerable<CardModel> cards)
	{
		Data data = GetInternalData<Data>();
		data.Cards.Clear();
		data.Cards.AddRange(cards);
	}

	/// <summary>
	/// 挂在 <see cref="BeforeHandDraw"/>（而非 <see cref="AfterPlayerTurnStart"/>），
	/// 保证获得全部牌发生在回合开始的正常抽牌之前。
	/// </summary>
	public override async Task BeforeHandDraw(
		Player player,
		PlayerChoiceContext choiceContext,
		ICombatState combatState)
	{
		if (Owner.Player is not { } owner || player != owner || Owner.IsDead)
		{
			return;
		}

		List<CardModel> eligible = LureAndRelease.GetPileEligibleCards(
			GetInternalData<Data>().Cards);
		if (eligible.Count == 0)
		{
			await PowerCmd.Remove(this);
			return;
		}

		Flash();
		foreach (CardModel card in eligible)
		{
			// 前一张牌进入手牌可能触发其它插入结算；每张牌加入前重新确认区域。
			if (LureAndRelease.IsInDrawOrDiscardPile(card))
			{
				await CardPileCmd.Add(card, PileType.Hand);
			}
		}

		await PowerCmd.Remove(this);
	}
}
