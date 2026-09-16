using System;
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
/// 夜观天象：不能被打出。进入弃牌堆时预见，数量为场上玩家与敌人总数（至多 5）。
/// 移动钩子统一记录入弃牌堆事件；有弃牌上下文时立即结算，否则延后至当前阵营回合结束。
/// </summary>
[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "stargazing")]
public sealed class Stargazing : ModCardTemplate
{
	public const int MaxScry = 5;

	private int _pendingDiscardPileEntries;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CreatureCountScryVar(),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Unplayable,
		SquKeywords.Scry,
	];

	public override int MaxUpgradeLevel => 0;

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
		while (_pendingDiscardPileEntries > 0)
		{
			await ConsumeOneDiscardPileEntry(choiceContext);
		}
	}

	private Task ConsumeOneDiscardPileEntry(PlayerChoiceContext choiceContext)
	{
		_pendingDiscardPileEntries--;

		SquSfx.PlayRandom(
			RunState,
			SquSfx.StargazingWangYunEvent,
			SquSfx.StargazingDongZhuoEvent);

		int amount = GetScryAmount(this);
		if (amount <= 0)
		{
			return Task.CompletedTask;
		}

		return ScryCmd.Execute(choiceContext, Owner, amount);
	}

	private static int GetScryAmount(CardModel card)
	{
		if (card.CombatState is not { } combatState)
		{
			return 0;
		}

		int count = combatState.Players.Count + combatState.Enemies.Count;
		return Math.Min(MaxScry, count);
	}

	private sealed class CreatureCountScryVar : DynamicVar
	{
		public CreatureCountScryVar()
			: base(ScryVar.VarName, 0m)
		{
		}

		public override void UpdateCardPreview(
			CardModel card,
			CardPreviewMode previewMode,
			Creature? target,
			bool runGlobalHooks)
		{
			int amount = GetScryAmount(card);
			BaseValue = amount;
			if (runGlobalHooks)
			{
				amount = ScryHook.ModifyScryAmount(card.Owner, amount, out _);
			}

			PreviewValue = amount;
		}
	}
}
