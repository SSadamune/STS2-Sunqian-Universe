using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Script;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

public abstract class WhatToEatCardBase : ModCardTemplate
{
	public const int ExhaustCount = 2;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/WhatToEat.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2),
	];

	protected WhatToEatCardBase()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await EatSomethingCardLogic.DrawAndExhaustFromHandAsync(
			choiceContext,
			Owner,
			this,
			DynamicVars.Cards.IntValue,
			ExhaustCount);

		await OnPlayMultiplayerExtrasAsync(choiceContext, cardPlay);
	}

	protected virtual Task OnPlayMultiplayerExtrasAsync(
		PlayerChoiceContext choiceContext,
		CardPlay cardPlay) => Task.CompletedTask;

	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(1m);
	}
}
