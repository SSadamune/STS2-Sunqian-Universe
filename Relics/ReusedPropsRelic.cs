using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using Squ;
using Squ.Character;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Relics;

/// <summary>
/// 复用素材：拾起时，从牌组中选择至多两张带消耗词条的剧本牌，永久移除其消耗词条。
/// </summary>
[RegisterRelic(typeof(SunqianRelicPool), StableEntryStem = "reused_props")]
public sealed class ReusedPropsRelic : ScriptRelicTemplate
{
	public override RelicRarity Rarity => RelicRarity.Shop;

	public override bool HasUponPickupEffect => true;

	public override RelicAssetProfile AssetProfile => new(
		IconPath: "res://images/relics/ReusedPropsRelic.png",
		IconOutlinePath: "res://images/relics/ReusedPropsRelicOutline.png",
		BigIconPath: "res://images/relics/ReusedPropsRelicBig.png");

	public override async Task AfterObtained()
	{
		IEnumerable<CardModel> selectedCards = await CardSelectCmd.FromDeckGeneric(
			Owner,
			new CardSelectorPrefs(SelectionScreenPrompt, 0, 2),
			IsEligibleScriptCard);

		bool removedAny = false;
		foreach (CardModel selectedCard in selectedCards)
		{
			CardCmd.RemoveKeyword(selectedCard, CardKeyword.Exhaust);
			removedAny = true;
		}

		if (removedAny)
		{
			Flash();
		}
	}

	private static bool IsEligibleScriptCard(CardModel card) =>
		card.Tags.Contains(SquCardTags.Script) && card.Keywords.Contains(CardKeyword.Exhaust);
}
