using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 仁义双剑：名字带有「打击」的牌额外打出 <see cref="Amount"/> 次。
/// </summary>
[RegisterPower]
public sealed class BenevolenceRighteousnessDualSwordsPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/BenevolenceRighteousnessDualSwordsPower.png",
		BigIconPath: "res://images/powers/BenevolenceRighteousnessDualSwordsPowerBig.png");

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (card.Owner.Creature != Owner || !StrikeNameMatcher.IsStrikeNamedCard(card))
		{
			return playCount;
		}

		return playCount + (int)Amount;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (card.Owner.Creature == Owner && StrikeNameMatcher.IsStrikeNamedCard(card))
		{
			Flash();
		}

		return Task.CompletedTask;
	}
}

internal static class StrikeNameMatcher
{
	public static bool IsStrikeNamedCard(CardModel card) =>
		card.Title.Contains("打击", StringComparison.Ordinal)
		|| card.Title.Contains("Strike", StringComparison.OrdinalIgnoreCase);
}
