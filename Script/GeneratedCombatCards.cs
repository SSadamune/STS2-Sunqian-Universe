using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

#nullable enable

namespace Squ.Script;

/// <summary>
/// 战斗内生成卡牌的工具方法（对齐 JACKPOT / BEGONE 的 CombatState + AddGeneratedCardToCombat 模式）。
/// </summary>
public static class GeneratedCombatCards
{
	public static CardModel CreateInCombat<T>(
		ICombatState combatState,
		Player player,
		bool upgraded)
		where T : CardModel
	{
		CardModel card = combatState.CreateCard<T>(player);
		if (upgraded)
		{
			card.UpgradeInternal();
			card.FinalizeUpgradeInternal();
		}

		return card;
	}

	public static async Task AddToHandInCombat<T>(
		ICombatState combatState,
		Player player,
		bool upgraded,
		Player? creator = null)
		where T : CardModel
	{
		CardModel card = CreateInCombat<T>(combatState, player, upgraded);
		await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, creator ?? player);
	}

	public static async Task AddToDrawPileInCombat<T>(
		ICombatState combatState,
		Player player,
		int count,
		bool upgraded,
		Player? creator = null)
		where T : CardModel
	{
		for (int i = 0; i < count; i++)
		{
			CardModel card = CreateInCombat<T>(combatState, player, upgraded);
			await CardPileCmd.AddGeneratedCardToCombat(
				card,
				PileType.Draw,
				creator ?? player,
				CardPilePosition.Random);
		}
	}

	/// <summary>
	/// 获取本地化后的卡牌标题（含升级后的「+」后缀）。
	/// </summary>
	public static string GetDisplayTitle<T>(bool upgraded)
		where T : CardModel
	{
		CardModel card = ModelDb.Card<T>().ToMutable();
		if (upgraded)
		{
			card.UpgradeInternal();
			card.FinalizeUpgradeInternal();
		}

		return card.Title;
	}
}
