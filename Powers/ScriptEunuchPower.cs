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
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class ScriptEunuchPower : ScriptPowerTemplate
{
	private sealed class Data
	{
		public HashSet<CardModel> MarkedCards { get; } = [];
	}

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptEunuchPower.png",
		BigIconPath: "res://images/powers/ScriptEunuchPowerBig.png");

	protected override object InitInternalData() => new Data();

	protected override Task OnScriptApplied(Creature? applier, CardModel? cardSource)
	{
		if (cardSource is not EunuchScript script)
		{
			return Task.CompletedTask;
		}

		Data data = GetInternalData<Data>();
		foreach (CardModel card in script.DrawnCardsForCurrentPlay)
		{
			if (card.Pile?.Type != PileType.Hand)
			{
				continue;
			}

			ApplyMark(card);
			data.MarkedCards.Add(card);
		}

		return Task.CompletedTask;
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
			List<CardModel> toExhaust = GetInternalData<Data>().MarkedCards
				.Where(card => card.Pile?.Type == PileType.Hand && card.HasEunuchMessage())
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
