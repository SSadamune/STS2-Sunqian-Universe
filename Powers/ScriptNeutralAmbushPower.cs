using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class ScriptNeutralAmbushPower : ScriptPowerTemplate
{
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptNeutralAmbushPower.png",
		BigIconPath: "res://images/powers/ScriptNeutralAmbushPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..HoverTipFactory.FromCardWithCardHoverTips<GiantRock>(false),
		..HoverTipFactory.FromCardWithCardHoverTips<SalvoStrike>(false),
	];

	public override async Task AfterCardDrawn(
		PlayerChoiceContext choiceContext,
		CardModel card,
		bool fromHandDraw)
	{
		if (Owner.IsDead || card.Owner != Owner.Player || !IsAmbushCard(card))
		{
			return;
		}

		Flash();
		await CardCmd.AutoPlay(choiceContext, card, null);
	}

	private static bool IsAmbushCard(CardModel card) =>
		card is GiantRock or SalvoStrike;
}
