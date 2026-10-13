using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Audio;
using Squ.Character;
using Squ.Patches;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "what_to_eat")]
public sealed class WhatToEat : ModCardTemplate
{
	public const int BaseDraw = 2;
	public const int UpgradedDraw = 3;
	public const int ExhaustCount = 2;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/WhatToEat.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(BaseDraw),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips
	{
		get
		{
			List<IHoverTip> tips = [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];
			if (ShouldShowMultiplayerContent)
			{
				tips.Add(HoverTipFactory.FromCard<ExactlyWhatToEat>(IsUpgraded));
			}

			return tips;
		}
	}

	public WhatToEat()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.WhatDoWeEatEvent);
		await EatSomethingCardLogic.ExhaustFromHandAndDrawAsync(
			choiceContext,
			Owner,
			this,
			DynamicVars.Cards.IntValue,
			ExhaustCount);

		await EatSomethingCardLogic.GrantExactlyWhatToEatToOtherPlayersAsync(
			Owner,
			this,
			IsUpgraded);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(UpgradedDraw - BaseDraw);
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		description.Add("IsMultiplayer", ShouldShowMultiplayerContent);
	}

	private bool ShouldShowMultiplayerContent => IsMutable && Owner is { } owner
		? owner.RunState.Players.Count > 1
		: CardLibraryMultiplayerPreviewState.ShowMultiplayerCards;
}
