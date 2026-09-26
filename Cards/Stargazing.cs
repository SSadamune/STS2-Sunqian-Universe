using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// 夜观天象：不能被打出。进入弃牌堆时预见 3（升级后 5）。
/// 移动钩子统一记录入弃牌堆事件；有弃牌上下文时立即结算，否则延后至当前阵营回合结束。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "stargazing")]
public sealed class Stargazing : ModCardTemplate
{
	public const int ScryAmount = 3;
	public const int UpgradedScryAmount = 5;

	private int _pendingDiscardPileEntries;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new ScryVar(ScryAmount),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Unplayable,
		SquKeywords.Scry,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/Stargazing.png");

	public Stargazing()
		: base(0, CardType.Skill, CardRarity.Uncommon, TargetType.None)
	{
	}

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
		Task.CompletedTask;

	public override Task AfterCardChangedPiles(
		CardModel card,
		PileType oldPileType,
		AbstractModel? clonedBy)
	{
		if (card == this
			&& oldPileType != PileType.Discard
			&& Pile?.Type == PileType.Discard)
		{
			_pendingDiscardPileEntries++;
		}

		return Task.CompletedTask;
	}

	public override Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
	{
		if (card != this || _pendingDiscardPileEntries <= 0)
		{
			return Task.CompletedTask;
		}

		return ConsumeOneDiscardPileEntry(choiceContext);
	}

	public override async Task AfterSideTurnEnd(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IEnumerable<Creature> participants)
	{
		if (CombatState is not { } combatState)
		{
			return;
		}

		List<Stargazing> ownerCopies = combatState
			.IterateHookListeners()
			.OfType<Stargazing>()
			.Where(card => card.Owner == Owner)
			.ToList();

		// 回合结束钩子会在一次玩家选择暂停后继续启动其他监听者。
		// 只让第一张牌负责串行结算，避免后续预见提前缓存相同的抽牌堆顶。
		if (ownerCopies.Count == 0 || ownerCopies[0] != this)
		{
			return;
		}

		foreach (Stargazing card in ownerCopies)
		{
			while (card._pendingDiscardPileEntries > 0)
			{
				await card.ConsumeOneDiscardPileEntry(choiceContext);
			}
		}
	}

	private Task ConsumeOneDiscardPileEntry(PlayerChoiceContext choiceContext)
	{
		_pendingDiscardPileEntries--;

		SquSfx.PlayRandom(
			RunState,
			SquSfx.StargazingWangYunEvent,
			SquSfx.StargazingDongZhuoEvent);

		return ScryCmd.Execute(choiceContext, this);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[ScryVar.VarName].UpgradeValueBy(UpgradedScryAmount - ScryAmount);
	}
}
