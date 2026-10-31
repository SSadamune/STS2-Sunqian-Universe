using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ.Audio;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

/// <summary>
/// 大声密谋：持有者打出群体目标攻击/技能牌，且实际合法目标数不大于一时，
/// 该牌额外结算等同于层数的次数（<see cref="ModifyCardPlayCount"/>）。
/// </summary>
[RegisterPower]
public sealed class LoudSecretPlotPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..base.AdditionalHoverTips,
		HoverTipFactory.FromKeyword(SquKeywords.MultiTarget),
	];

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/LoudSecretPlotPower.png",
		BigIconPath: "res://images/powers/LoudSecretPlotPowerBig.png");

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (card.Owner.Creature != Owner || !ShouldReplay(card))
		{
			return playCount;
		}

		return playCount + Amount;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (card.Owner.Creature == Owner && ShouldReplay(card))
		{
			Flash();
			SquSfx.Play(SquSfx.TraitorDongZhuoEvent);
		}

		return Task.CompletedTask;
	}

	private bool ShouldReplay(CardModel card)
	{
		if (!IsQualifyingCardType(card) || !HasMultiTargetIntent(card))
		{
			return false;
		}

		ICombatState? combatState = CombatState;
		if (combatState is null)
		{
			return false;
		}

		int targetCount = card.TargetType == TargetType.AllAllies
			? combatState.GetTeammatesOf(card.Owner.Creature).Count(creature => creature.IsAlive && creature.IsPlayer)
			: combatState.HittableEnemies.Count;
		return targetCount <= 1;
	}

	private static bool IsQualifyingCardType(CardModel card) =>
		card.Type is CardType.Attack or CardType.Skill;

	private static bool HasMultiTargetIntent(CardModel card) =>
		MultiTargetCardIntent.HasMultiTargetIntent(card);
}
