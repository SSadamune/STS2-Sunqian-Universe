using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Character;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "what_to_eat")]
public sealed class WhatToEat : ModCardTemplate
{
	public const int ExhaustCount = 2;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/WhatToEat.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromCard<ExactlyWhatToEat>(IsUpgraded),
	];

	public WhatToEat()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		int drawCount = DynamicVars.Cards.IntValue;
		await EatSomethingCardLogic.DrawAndExhaustFromHandAsync(
			choiceContext,
			Owner,
			this,
			drawCount,
			ExhaustCount);

		await EatSomethingCardLogic.GrantExactlyWhatToEatToOtherPlayersAsync(Owner, this, IsUpgraded);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(1m);
	}
}
