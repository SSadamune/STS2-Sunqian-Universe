using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Squ;
using Squ.Script;
using Squ.Cards;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class SunqianUniversePower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override Color AmountLabelColor => PowerModel._normalAmountLabelColor;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/SunqianUniversePower.png",
		BigIconPath: "res://images/powers/SunqianUniversePowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(SquKeywords.ScriptCollectionId.GetModCardKeyword()),
	];

	public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
	{
		int drawCount = Amount;
		if (drawCount <= 0 || Owner.Player is not { } player || card.Owner != player
			|| oldPileType == ScriptSystem.ScriptCollectionPile
			|| card.Pile?.Type != ScriptSystem.ScriptCollectionPile)
		{
			return;
		}

		Flash();
		await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), drawCount, player);
	}
}
