using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 「鸡脚芝士」：在持续回合内，基础 <see cref="CardTag.Strike"/> 牌改为随机两名敌人目标。
/// </summary>
[RegisterPower]
public sealed class ChickenFootCheeseStrikePower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerInstanceType InstanceType => PowerInstanceType.None;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ChickenFootCheeseStrikePower.png",
		BigIconPath: "res://images/powers/ChickenFootCheeseStrikePowerBig.png");

	public const int RedirectRandomEnemyCount = 2;

	public static bool ShouldRedirectBasicStrike(CardModel card)
	{
		if (card.Owner?.Creature?.GetPower<ChickenFootCheeseStrikePower>() is not { Amount: > 0 })
		{
			return false;
		}

		return card.Rarity == CardRarity.Basic && card.Tags.Contains(CardTag.Strike);
	}

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		SquStrikeRedirectPatches.EnsureApplied();
		return Task.CompletedTask;
	}

	public override async Task AfterSideTurnEnd(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IEnumerable<Creature> participants)
	{
		if (side != Owner.Side || !participants.Contains(Owner) || Amount <= 0)
		{
			return;
		}

		Flash();
		await PowerCmd.TickDownDuration(this);
	}
}
