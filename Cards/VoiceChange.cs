using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Squ;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "voice_change")]
public sealed class VoiceChange : ModCardTemplate
{
	private static readonly LocString SelectionPrompt =
		new("cards", "SUNQIAN_UNIVERSE_CARD_VOICE_CHANGE.selectionScreenPrompt");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(5m, ValueProp.Move),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/VoiceChange.png");

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		CardKeyword.Exhaust,
	];

	public VoiceChange()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		CardPile exhaustPile = PileType.Exhaust.GetPile(Owner);
		CardSelectorPrefs prefs = new(SelectionPrompt, 1);
		IEnumerable<CardModel> selected = await CardSelectCmd.FromCombatPile(
			choiceContext,
			exhaustPile,
			Owner,
			prefs,
			IsScriptCard);

		CardModel? scriptCard = selected.FirstOrDefault();
		if (scriptCard is not null)
		{
			await CardPileCmd.Add(scriptCard, PileType.Hand);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
		RemoveKeyword(CardKeyword.Exhaust);
	}

	private static bool IsScriptCard(CardModel card) =>
		card.Tags.Contains(SquCardTags.Script);
}
