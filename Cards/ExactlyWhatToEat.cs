using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Squ.Audio;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(TokenCardPool), StableEntryStem = "exactly_what_to_eat")]
public sealed class ExactlyWhatToEat : ModCardTemplate
{
	public const int ExhaustCount = 1;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/ExactlyWhatToEat.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
	];

	public ExactlyWhatToEat()
		: base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.ExactlyWhatToEatEvent);
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
