using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Character;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "exactly_what_to_eat")]
public sealed class ExactlyWhatToEat : ModCardTemplate
{
	public const int ExhaustCount = 1;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/ExactlyWhatToEat.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1),
	];

	public ExactlyWhatToEat()
		: base(0, CardType.Skill, CardRarity.Token, TargetType.Self, false)
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
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(1m);
	}
}
