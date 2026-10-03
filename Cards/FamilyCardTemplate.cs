using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ.Audio;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// Shared metadata, keywords, audio hooks, and optional discard handling for generated Family
/// Quest cards. Explicit discard effects resolve through
/// <see cref="AfterCardDiscarded"/> immediately. End-of-turn hand flushing has no discard context,
/// so pile entries are queued and resolved after the flush.
/// </summary>
public abstract class FamilyCardTemplate : ModCardTemplate
{
	private int _pendingDiscards;

	protected virtual bool ResolvesEffectsWhenDiscarded => false;

	protected virtual string? DiscardSfxEvent => null;

	protected virtual string? ExhaustSfxEvent => null;

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Unplayable,
		SquKeywords.Family,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[SquCommonL10n.PlayRateHoverTip()];

	protected FamilyCardTemplate(int energyCost = -1)
		: base(energyCost, CardType.Quest, CardRarity.Quest, TargetType.None, showInCardLibrary: false)
	{
	}

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
		Task.CompletedTask;

	protected virtual Task ResolveAdditionalDiscard(PlayerChoiceContext choiceContext) =>
		Task.CompletedTask;

	protected virtual Task ResolveAdditionalExhaust(
		PlayerChoiceContext choiceContext,
		bool causedByEthereal) =>
		Task.CompletedTask;

	public override Task AfterCardChangedPiles(
		CardModel card,
		PileType oldPileType,
		AbstractModel? clonedBy)
	{
		if (ResolvesEffectsWhenDiscarded
			&& card == this
			&& oldPileType is PileType.Hand or PileType.Draw
			&& Pile?.Type == PileType.Discard)
		{
			_pendingDiscards++;
		}

		return Task.CompletedTask;
	}

	public override Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
	{
		if (card != this || _pendingDiscards <= 0)
		{
			return Task.CompletedTask;
		}

		return ResolveOneDiscard(choiceContext);
	}

	public override async Task AfterCardExhausted(
		PlayerChoiceContext choiceContext,
		CardModel card,
		bool causedByEthereal)
	{
		if (card != this)
		{
			return;
		}

		if (ExhaustSfxEvent is { } eventPath)
		{
			SquSfx.Play(eventPath);
		}

		await ResolveAdditionalExhaust(choiceContext, causedByEthereal);
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

		List<FamilyCardTemplate> ownerCopies = combatState
			.IterateHookListeners()
			.OfType<FamilyCardTemplate>()
			.Where(card => card.Owner == Owner)
			.ToList();

		if (ownerCopies.Count == 0 || ownerCopies[0] != this)
		{
			return;
		}

		foreach (FamilyCardTemplate card in ownerCopies)
		{
			while (card._pendingDiscards > 0)
			{
				await card.ResolveOneDiscard(choiceContext);
			}
		}
	}

	private async Task ResolveOneDiscard(PlayerChoiceContext choiceContext)
	{
		_pendingDiscards--;
		if (DiscardSfxEvent is { } eventPath)
		{
			SquSfx.Play(eventPath);
		}

		await ResolveAdditionalDiscard(choiceContext);
	}
}
