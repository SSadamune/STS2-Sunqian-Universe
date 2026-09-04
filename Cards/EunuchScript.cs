using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "eunuch_script")]
public sealed class EunuchScript : ScriptCardTemplate
{
	public const int BaseDraw = 2;
	public const int UpgradedDraw = 3;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(BaseDraw),
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Retain),
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
	];

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Script,
		CardKeyword.Exhaust,
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/EunuchScript.png");

	public EunuchScript()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self, true)
	{
	}

	protected override async Task PlayScriptAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		List<CardModel> drawn = (await CardPileCmd.Draw(
			choiceContext,
			DynamicVars.Cards.BaseValue,
			Owner)).ToList();

		await PowerCmd.Apply<ScriptEunuchPower>(
			choiceContext,
			Owner.Creature,
			1m,
			Owner.Creature,
			this);
		ScriptEunuchPower.MarkDrawnCards(drawn);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(UpgradedDraw - BaseDraw);
	}
}
