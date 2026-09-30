using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Squ;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "the_backups_backup")]
public sealed class TheBackupsBackup : ModCardTemplate
{
	private const int ChoiceCount = 3;

	private static readonly CardRarity[] BaseDiscoverableRarities =
	[
		CardRarity.Common,
		CardRarity.Uncommon,
		CardRarity.Rare,
		CardRarity.Ancient,
		CardRarity.Event,
	];

	private static readonly CardRarity[] UpgradedDiscoverableRarities =
	[
		CardRarity.Uncommon,
		CardRarity.Rare,
		CardRarity.Ancient,
		CardRarity.Event,
	];

	public override CardMultiplayerConstraint MultiplayerConstraint =>
		CardMultiplayerConstraint.MultiplayerOnly;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		CreateHiddenResolutionTip(),
	];

	public TheBackupsBackup()
		: base(0, CardType.Skill, CardRarity.Uncommon, SquTargetTypes.AnyOtherPlayer)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		ICombatState combatState = CombatState
			?? throw new InvalidOperationException("TheBackupsBackup requires an active combat.");
		Player ally = cardPlay.Target.Player
			?? throw new InvalidOperationException("TheBackupsBackup requires another player as its target.");

		await ChooseAndGainCopy(
			choiceContext,
			combatState,
			chooser: Owner,
			sourceDeckOwner: ally,
			recipient: Owner);

		await ChooseAndGainCopy(
			choiceContext,
			combatState,
			chooser: ally,
			sourceDeckOwner: Owner,
			recipient: ally);
	}

	private async Task ChooseAndGainCopy(
		PlayerChoiceContext choiceContext,
		ICombatState combatState,
		Player chooser,
		Player sourceDeckOwner,
		Player recipient)
	{
		CardRarity[] allowed = IsUpgraded ? UpgradedDiscoverableRarities : BaseDiscoverableRarities;
		List<CardModel> options = sourceDeckOwner.Deck.Cards
			.Where(card => allowed.Contains(card.Rarity))
			.ToList();
		sourceDeckOwner.RunState.Rng.CombatCardGeneration.Shuffle(options);
		if (options.Count > ChoiceCount)
		{
			options.RemoveRange(ChoiceCount, options.Count - ChoiceCount);
		}

		List<CardModel> fillers = FillOptionsFromCardPool(
			combatState,
			sourceDeckOwner,
			recipient,
			options,
			allowed);
		sourceDeckOwner.RunState.Rng.CombatCardGeneration.Shuffle(options);

		if (options.Count == 0)
		{
			return;
		}

		CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
			choiceContext,
			options,
			chooser);
		if (selected is not null && fillers.Contains(selected))
		{
			await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner);
		}
		else if (selected is not null)
		{
			CardModel copy = CardModel.FromSerializable(selected.ToSerializable());
			combatState.AddCard(copy, recipient);
			await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
		}

		foreach (CardModel filler in fillers)
		{
			if (!ReferenceEquals(filler, selected))
			{
				combatState.RemoveCard(filler);
			}
		}
	}

	private static List<CardModel> FillOptionsFromCardPool(
		ICombatState combatState,
		Player sourceDeckOwner,
		Player recipient,
		List<CardModel> options,
		CardRarity[] allowed)
	{
		int missing = ChoiceCount - options.Count;
		if (missing <= 0)
		{
			return [];
		}

		HashSet<CardModel> offered = options.Select(card => card.CanonicalInstance).ToHashSet();
		List<CardModel> pool = sourceDeckOwner.Character.CardPool
			.GetUnlockedCards(
				sourceDeckOwner.UnlockState,
				sourceDeckOwner.RunState.CardMultiplayerConstraint)
			.Where(card => allowed.Contains(card.Rarity) && !offered.Contains(card.CanonicalInstance))
			.DistinctBy(card => card.CanonicalInstance)
			.ToList();
		if (pool.Count == 0)
		{
			return [];
		}

		List<CardModel> fillers = pool
			.TakeRandom(Math.Min(missing, pool.Count), sourceDeckOwner.RunState.Rng.CombatCardGeneration)
			.Select(canonical => combatState.CreateCard(canonical, recipient))
			.ToList();
		options.AddRange(fillers);
		return fillers;
	}

	private IHoverTip CreateHiddenResolutionTip()
	{
		string suffix = IsUpgraded ? "hiddenUpgraded" : "hidden";
		return new HoverTip(
			SquCommonL10n.AnnotationTitle(),
			new LocString("cards", Id.Entry + "." + suffix));
	}
}
