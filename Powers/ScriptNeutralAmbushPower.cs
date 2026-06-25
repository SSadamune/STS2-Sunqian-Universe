using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using Squ.Cards;
using Squ.Script;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Powers;

[RegisterPower]
public sealed class ScriptNeutralAmbushPower : ScriptPowerTemplate
{
	public const string GeneratedCardVarName = "GeneratedCard";

	private sealed class Data
	{
		public bool GrantUpgradedBoulder;
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
		..HoverTipFactory.FromCardWithCardHoverTips<GiantRock>(
			GetInternalData<Data>().GrantUpgradedBoulder),
	];

	protected override object InitInternalData() => new Data();

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		bool upgraded = cardSource is NeutralAmbushScript { IsUpgraded: true };
		Data data = GetInternalData<Data>();
		data.GrantUpgradedBoulder = upgraded;
		((StringVar)DynamicVars[GeneratedCardVarName]).StringValue =
			$"[gold]{GeneratedCombatCards.GetDisplayTitle<GiantRock>(upgraded)}[/gold]";
		return Task.CompletedTask;
	}

	public override async Task AfterCardDrawn(
		PlayerChoiceContext choiceContext,
		CardModel card,
		bool fromHandDraw)
	{
		if (Owner.IsDead || card.Owner != Owner.Player || card is not GiantRock)
		{
			return;
		}

		Flash();
		await CardCmd.AutoPlay(choiceContext, card, null);
	}
}
