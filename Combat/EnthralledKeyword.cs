using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

#nullable enable

namespace Squ.Combat;

/// <summary>
/// 「执迷」词条：对齐原版诅咒 <see cref="Enthralled"/> 的 <c>ShouldPlay</c> 投票。
/// 手牌里只有一张执迷类牌时，必须先打它；出现多张词条牌、或词条牌与诅咒并存时，
/// 词条不再拦其它执迷类牌。诅咒本体不改，只在其 <c>ShouldPlay</c> 上放行带词条的牌。
/// </summary>
public static class EnthralledKeyword
{
	public static bool IsFamily(CardModel card) =>
		card is Enthralled || card.Keywords.Contains(SquKeywords.Enthralled);

	public static bool AllowsPlay(CardModel listener, CardModel card, AutoPlayType autoPlayType)
	{
		if (card.Owner != listener.Owner)
		{
			return true;
		}

		if (listener.Pile?.Type != PileType.Hand)
		{
			return true;
		}

		if (autoPlayType != AutoPlayType.None)
		{
			return true;
		}

		if (IsFamily(card))
		{
			if (CountFamilyInHand(listener.Owner) > 1)
			{
				return true;
			}

			return card == listener;
		}

		return false;
	}

	private static int CountFamilyInHand(Player player) =>
		PileType.Hand.GetPile(player).Cards.Count(IsFamily);
}
