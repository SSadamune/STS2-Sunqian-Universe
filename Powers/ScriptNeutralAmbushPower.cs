using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using Squ.Audio;
using Squ.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class ScriptNeutralAmbushPower : ScriptPowerTemplate
{
	private sealed class Data
	{
		public bool TriggeredThisTurn;
	}

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://images/powers/ScriptNeutralAmbushPower.png",
		BigIconPath: "res://images/powers/ScriptNeutralAmbushPowerBig.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		..HoverTipFactory.FromCardWithCardHoverTips<GiantRock>(false),
		..HoverTipFactory.FromCardWithCardHoverTips<SalvoStrike>(false),
	];

	protected override object InitInternalData() => new Data();

	public override Task AfterSideTurnStart(
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (side == Owner.Side && participants.Contains(Owner))
		{
			GetInternalData<Data>().TriggeredThisTurn = false;
		}

		return Task.CompletedTask;
	}

	public override async Task AfterCardDrawn(
		PlayerChoiceContext choiceContext,
		CardModel card,
		bool fromHandDraw)
	{
		Data data = GetInternalData<Data>();
		if (Owner.IsDead
			|| card.Owner != Owner.Player
			|| !IsAmbushCard(card)
			|| data.TriggeredThisTurn)
		{
			return;
		}

		data.TriggeredThisTurn = true;
		SquSfx.Play(SquSfx.NeutralAmbushTriggerEvent);
		Flash();
		await CardCmd.AutoPlay(choiceContext, card, null);
	}

	private static bool IsAmbushCard(CardModel card) =>
		card is GiantRock or SalvoStrike;
}
