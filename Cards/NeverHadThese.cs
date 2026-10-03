using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Squ.Audio;
using Squ.Character;
using Squ.Powers;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "never_had_these")]
public sealed class NeverHadThese : ModCardTemplate
{
	public const int EnergyGain = 1;
	public const int DexterityGain = 1;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<DexterityPower>(DexterityGain),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromCard<BelovedPartner>(),
		HoverTipFactory.FromCard<CutePet>(),
		HoverTipFactory.FromCard<NextOfKin>(),
		HoverTipFactory.FromPower<DexterityPower>(),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/NeverHadThese.png");

	public NeverHadThese()
		: base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.Play(SquSfx.NeverHadThesePlayEvent);

		if (CombatState is not { } combatState)
		{
			return;
		}

		List<CardModel> choices =
		[
			GeneratedCombatCards.CreateInCombat<BelovedPartner>(combatState, Owner, upgraded: false),
			GeneratedCombatCards.CreateInCombat<CutePet>(combatState, Owner, upgraded: false),
			GeneratedCombatCards.CreateInCombat<NextOfKin>(combatState, Owner, upgraded: false),
		];

		CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
			choiceContext,
			choices,
			Owner,
			canSkip: false);
		selected ??= choices[0];

		await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner);

		CardPile drawPile = PileType.Draw.GetPile(Owner);
		foreach (CardModel other in choices.Where(card => card != selected))
		{
			CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(
				other,
				PileType.Draw,
				Owner,
				CardPilePosition.Random);
			if (result.success)
			{
				drawPile.InvokeCardAddFinished();
			}
		}

		await PowerCmd.Apply<NeverHadThesePower>(
			choiceContext,
			Owner.Creature,
			EnergyGain,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Innate);
	}
}
