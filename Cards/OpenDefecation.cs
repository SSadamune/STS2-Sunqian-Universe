using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "open_defecation")]
public sealed class OpenDefecation : ModCardTemplate
{
	private const int Threshold = 3;
	private const decimal BaseDexterity = 3m;
	private const decimal UpgradedDexterity = 4m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Threshold", Threshold),
		new PowerVar<DexterityPower>(BaseDexterity),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<DexterityPower>(),
		HoverTipFactory.FromKeyword(CardKeyword.Unplayable),
		HoverTipFactory.FromKeyword(SquKeywords.CountsAsPlayed),
		HoverTipFactory.FromCard<Splash>(IsUpgraded),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/OpenDefecation.png");

	protected override bool ShouldGlowGoldInternal =>
		Pile?.Type == PileType.Hand
		&& PileType.Draw.GetPile(Owner).Cards.Count(HasUnplayableKeyword) >= Threshold;

	public OpenDefecation()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ICombatState? combatState = CombatState;
		ArgumentNullException.ThrowIfNull(combatState, nameof(combatState));

		await PowerCmd.Apply<TempDexFromOpenDefecationPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars[nameof(DexterityPower)].BaseValue,
			Owner.Creature,
			this);

		List<CardModel> unplayableCards = PileType.Draw.GetPile(Owner).Cards
			.Where(HasUnplayableKeyword)
			.ToList();

		foreach (CardModel card in unplayableCards)
		{
			await CardPileCmd.Add(card, PileType.Discard);
		}

		if (unplayableCards.Count >= Threshold)
		{
			CardModel splashSource = combatState.CreateCard<Splash>(Owner);
			if (IsUpgraded)
			{
				splashSource.UpgradeInternal();
				splashSource.FinalizeUpgradeInternal();
			}

			CardModel splashDupe = splashSource.CreateDupe(Owner);
			splashSource.RemoveFromState();
			await CardCmd.AutoPlay(
				choiceContext,
				splashDupe,
				target: null,
				skipCardPileVisuals: true);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars[nameof(DexterityPower)]
			.UpgradeValueBy(UpgradedDexterity - BaseDexterity);
	}

	private static bool HasUnplayableKeyword(CardModel card) =>
		card.Keywords.Contains(CardKeyword.Unplayable);
}
