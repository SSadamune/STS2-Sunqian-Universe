using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 可叠层剧本：夜袭乌巢。<see cref="CardTag.Strike"/> 卡牌攻击后，额外给予目标 <see cref="Amount"/> 层灼烧。
/// 叠层时 <see cref="Amount"/> 累加（未升级 +4，升级 +6）。
/// </summary>
[RegisterPower]
public sealed class ScriptNightRaidWuchaoPower : StackableScriptPowerTemplate
{
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptNightRaidWuchaoPower.png",
		BigIconPath: "res://images/powers/ScriptNightRaidWuchaoPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<BurningPower>(NightRaidWuchaoScript.BaseBurning),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..base.AdditionalHoverTips,
		HoverTipFactory.FromPower<BurningPower>(),
	];

	protected override void OnStackedFrom(CardModel? cardSource)
	{
		SyncDisplayVars();
	}

	public override Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (power == this && amount > 0m)
		{
			OnStackedFrom(cardSource);
		}

		return Task.CompletedTask;
	}

	public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
	{
		if (Owner.IsDead
			|| Amount <= 0m
			|| !TryGetStrikeAttackCard(command, out CardModel strikeCard))
		{
			return;
		}

		List<Creature> targets = command.Results
			.SelectMany(results => results)
			.Select(result => result.Receiver)
			.Where(creature => creature.IsAlive)
			.Distinct()
			.ToList();
		if (targets.Count == 0)
		{
			return;
		}

		Flash();

		foreach (Creature target in targets)
		{
			await PowerCmd.Apply<BurningPower>(
				choiceContext,
				target,
				Amount,
				Owner,
				strikeCard);
		}
	}

	private bool TryGetStrikeAttackCard(AttackCommand command, out CardModel card)
	{
		card = null!;
		if (command.Attacker != Owner)
		{
			return false;
		}

		if (command.ModelSource is not CardModel cardSource)
		{
			return false;
		}

		if (cardSource.Owner.Creature != Owner || !cardSource.Tags.Contains(CardTag.Strike))
		{
			return false;
		}

		card = cardSource;
		return true;
	}

	private void SyncDisplayVars() =>
		DynamicVars[nameof(BurningPower)].BaseValue = Amount;
}
