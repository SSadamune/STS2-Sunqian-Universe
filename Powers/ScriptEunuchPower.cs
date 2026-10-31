using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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

	protected override Task OnScriptApplied(Creature? applier, CardModel? cardSource) =>
		Task.CompletedTask;

	public override bool ShouldEtherealTrigger(CardModel card) =>
		!TreatsEtherealAsRetain(card);

	public static bool TreatsEtherealAsRetain(CardModel card) =>
		card.Pile?.Type == PileType.Hand
		&& card.Keywords.Contains(CardKeyword.Ethereal)
		&& card.Owner?.Creature.GetPower<ScriptEunuchPower>() is not null;

	internal static void ApplyTemporaryEthereal(CardModel card)
	{
		if (card.Keywords.Contains(CardKeyword.Ethereal))
		{
			return;
		}

		CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
		CardCmd.ApplyKeyword(card, SquKeywords.EunuchMessage);
	}

	internal static void ClearTemporaryEtherealIfPresent(CardModel card)
	{
		if (!card.HasEunuchMessage())
		{
			return;
		}

		CardCmd.RemoveKeyword(card, CardKeyword.Ethereal);
		CardCmd.RemoveKeyword(card, SquKeywords.EunuchMessage);
	}
}
