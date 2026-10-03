using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ.Audio;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

/// <summary>
/// Shared discard handling for generated Family Quest cards. Explicit discards resolve through
/// <see cref="AfterCardDiscarded"/> immediately. End-of-turn hand flushing has no discard context,
/// so pile entries are queued and resolved after the flush; cards drawn by these effects therefore
/// remain in hand.
/// </summary>
public abstract class FamilyCardTemplate : ModCardTemplate
{
	private int _pendingDiscards;

	protected abstract int BlockAmount { get; }

	protected abstract int DrawCards { get; }

	protected virtual string? DiscardSfxEvent => null;

	protected virtual string? ExhaustSfxEvent => null;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get
		{
			if (BlockAmount > 0)
			{
				yield return new BlockVar(BlockAmount, ValueProp.Move);
			}

			if (DrawCards > 0)
			{
				yield return new CardsVar(DrawCards);
			}
		}
	}

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Unplayable,
		SquKeywords.Family,
	];

	public override bool GainsBlock => BlockAmount > 0;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		BlockAmount > 0
			? [SquCommonL10n.PlayRateHoverTip(), HoverTipFactory.Static(StaticHoverTip.Block)]
			: [SquCommonL10n.PlayRateHoverTip()];

	protected FamilyCardTemplate(int energyCost = -1)
		: base(energyCost, CardType.Quest, CardRarity.Quest, TargetType.None, showInCardLibrary: false)
	{
	}

	protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
		Task.CompletedTask;

	protected virtual Task ResolveAdditionalDiscard(PlayerChoiceContext choiceContext) =>
		Task.CompletedTask;

	public override Task AfterCardChangedPiles(
		CardModel card,
		PileType oldPileType,
		AbstractModel? clonedBy)
	{
		if (card == this
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

	public override Task AfterCardExhausted(
		PlayerChoiceContext choiceContext,
		CardModel card,
		bool causedByEthereal)
	{
		if (card == this && ExhaustSfxEvent is { } eventPath)
		{
			SquSfx.Play(eventPath);
		}

		return Task.CompletedTask;
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

		if (BlockAmount > 0)
		{
			await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay: null);
		}

		if (DrawCards > 0)
		{
			await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
		}
	}
}
