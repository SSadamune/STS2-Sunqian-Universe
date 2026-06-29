using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class ScriptNeutralAmbushPower : StackableScriptPowerTemplate
{
	public const string GeneratedCardVarName = "GeneratedCard";

	private const int DrawAfterAutoPlay = 1;

	private sealed class Data
	{
		public int AutoPlayedOnTurnNumber = -1;

		public int AutoPlayedCountThisTurn;
	}

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptNeutralAmbushPower.png",
		BigIconPath: "res://images/powers/ScriptNeutralAmbushPowerBig.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new StringVar(GeneratedCardVarName, GeneratedCombatCards.GetDisplayTitle<GiantRock>(upgraded: false)),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..HoverTipFactory.FromCardWithCardHoverTips<GiantRock>(false),
		..base.AdditionalHoverTips,
	];

	protected override object InitInternalData() => new Data();

	protected override void OnStackedFrom(CardModel? cardSource)
	{
	}

	public override async Task AfterCardDrawn(
		PlayerChoiceContext choiceContext,
		CardModel card,
		bool fromHandDraw)
	{
		if (Owner.IsDead || card.Owner != Owner.Player || card is not GiantRock || Amount <= 0m)
		{
			return;
		}

		Player? player = Owner.Player;
		if (player is null)
		{
			return;
		}

		Data data = GetInternalData<Data>();
		int turnNumber = player.PlayerCombatState?.TurnNumber ?? -1;
		if (data.AutoPlayedOnTurnNumber != turnNumber)
		{
			data.AutoPlayedOnTurnNumber = turnNumber;
			data.AutoPlayedCountThisTurn = 0;
		}

		if (data.AutoPlayedCountThisTurn >= Amount)
		{
			return;
		}

		Flash();
		await CardCmd.AutoPlay(choiceContext, card, null);
		data.AutoPlayedCountThisTurn++;
		await CardPileCmd.Draw(choiceContext, DrawAfterAutoPlay, player);
	}
}
