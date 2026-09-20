using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ.Character;
using Squ.Combat;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "drinking_table_culture")]
public sealed class DrinkingTableCulture : ModCardTemplate
{
	public override CardMultiplayerConstraint MultiplayerConstraint =>
		CardMultiplayerConstraint.MultiplayerOnly;

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.Enthralled),
		HoverTipFactory.FromCard<Wine>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/DrinkingTableCulture.png");

	public DrinkingTableCulture()
		: base(1, CardType.Skill, CardRarity.Uncommon, SquTargetTypes.AnyOtherPlayer)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

		ICombatState combatState = CombatState
			?? throw new InvalidOperationException("DrinkingTableCulture requires an active combat.");
		Player targetPlayer = cardPlay.Target.Player
			?? throw new InvalidOperationException("DrinkingTableCulture requires a player target.");

		await GeneratedCombatCards.AddToHandInCombat<Wine>(
			combatState,
			targetPlayer,
			upgraded: false,
			Owner,
			ApplyEnthralled);
		await GeneratedCombatCards.AddToDrawPileInCombat<Wine>(
			combatState,
			targetPlayer,
			1,
			upgraded: false,
			Owner,
			ApplyEnthralled);
	}

	protected override void OnUpgrade()
	{
		EnergyCost.UpgradeBy(-1);
	}

	private static void ApplyEnthralled(CardModel card) =>
		CardCmd.ApplyKeyword(card, SquKeywords.Enthralled);
}
