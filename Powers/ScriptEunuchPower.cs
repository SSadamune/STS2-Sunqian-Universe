using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Squ;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class ScriptEunuchPower : ScriptPowerTemplate
{
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptEunuchPower.png",
		BigIconPath: "res://images/powers/ScriptEunuchPowerBig.png");

	public static void MarkDrawnCards(IEnumerable<CardModel> cards)
	{
		foreach (CardModel card in cards)
		{
			if (card.Pile?.Type == PileType.Hand)
			{
				ApplyMark(card);
			}
		}
	}

	public static void ClearMarkIfPresent(CardModel card)
	{
		if (card.HasEunuchMessage())
		{
			CardCmd.RemoveKeyword(card, SquKeywords.EunuchMessage);
		}
	}

	public override async Task AfterRemoved(Creature oldOwner)
	{
		Player? player = oldOwner.Player;
		if (player is not null
			&& oldOwner.CombatState is not null
			&& !CombatManager.Instance.IsOverOrEnding)
		{
			List<CardModel> toExhaust = PileType.Hand.GetPile(player).Cards
				.Where(static card => card.HasEunuchMessage())
				.ToList();
			foreach (CardModel card in toExhaust)
			{
				await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(), card);
			}
		}

		await base.AfterRemoved(oldOwner);
	}

	private static void ApplyMark(CardModel card)
	{
		if (!card.HasEunuchMessage())
		{
			CardCmd.ApplyKeyword(card, SquKeywords.EunuchMessage);
		}
	}
}
