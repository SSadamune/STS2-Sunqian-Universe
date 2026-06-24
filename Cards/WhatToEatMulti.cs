using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ.Character;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "what_to_eat_multi")]
public sealed class WhatToEatMulti : WhatToEatCardBase
{
	public override CardMultiplayerConstraint MultiplayerConstraint =>
		CardMultiplayerConstraint.MultiplayerOnly;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromCard<ExactlyWhatToEat>(IsUpgraded),
	];

	protected override Task OnPlayMultiplayerExtrasAsync(
		PlayerChoiceContext choiceContext,
		CardPlay cardPlay) =>
		EatSomethingCardLogic.GrantExactlyWhatToEatToOtherPlayersAsync(Owner, this, IsUpgraded);
}
