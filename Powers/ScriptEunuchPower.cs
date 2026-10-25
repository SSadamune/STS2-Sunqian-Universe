using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ;
using Squ.Cards;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class ScriptEunuchPower : ScriptPowerTemplate
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(EunuchScript.BaseDraw),
	];

	private sealed class Data
	{
		public HashSet<CardModel> MarkedCards { get; } = [];
	}

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptEunuchPower.png",
		BigIconPath: "res://images/powers/ScriptEunuchPowerBig.png");

	protected override object InitInternalData() => new Data();

	protected override async Task OnScriptApplied(Creature? applier, CardModel? cardSource)
	{
		if (Owner.Player is not { } player)
		{
			return;
		}

		Data data = GetInternalData<Data>();
		int draw = cardSource is EunuchScript { IsUpgraded: true }
			? EunuchScript.UpgradedDraw
			: EunuchScript.BaseDraw;
		DynamicVars.Cards.BaseValue = draw;
		List<CardModel> drawn = (await CardPileCmd.Draw(
			new ThrowingPlayerChoiceContext(),
			draw,
			player)).ToList();
		foreach (CardModel card in drawn.Where(card => card.Pile?.Type == PileType.Hand))
		{
			ApplyMark(card);
			data.MarkedCards.Add(card);
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
		if (!SuppressScriptEndEffects
			&& player is not null
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

	internal static void ApplyMark(CardModel card)
	{
		if (!card.HasEunuchMessage())
		{
			CardCmd.ApplyKeyword(card, SquKeywords.EunuchMessage);
		}
	}

}
