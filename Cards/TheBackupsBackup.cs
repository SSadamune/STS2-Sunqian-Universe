using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
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

	public override CardMultiplayerConstraint MultiplayerConstraint =>
		CardMultiplayerConstraint.MultiplayerOnly;

	public TheBackupsBackup()
		: base(1, CardType.Skill, CardRarity.Uncommon, SquTargetTypes.AnyOtherPlayer)
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

	protected override void OnUpgrade()
	{
		EnergyCost.UpgradeBy(-1);
	}

	private async Task ChooseAndGainCopy(
		PlayerChoiceContext choiceContext,
		ICombatState combatState,
		Player chooser,
		Player sourceDeckOwner,
		Player recipient)
	{
		List<CardModel> options = sourceDeckOwner.Deck.Cards.ToList();
		sourceDeckOwner.RunState.Rng.CombatCardGeneration.Shuffle(options);
		if (options.Count > ChoiceCount)
		{
			options.RemoveRange(ChoiceCount, options.Count - ChoiceCount);
		}

		if (options.Count == 0)
		{
			return;
		}

		CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
			choiceContext,
			options,
			chooser);
		if (selected is null)
		{
			return;
		}

		CardModel copy = CardModel.FromSerializable(selected.ToSerializable());
		combatState.AddCard(copy, recipient);
		await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
	}
}
