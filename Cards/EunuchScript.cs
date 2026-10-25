using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Squ;
using Squ.Audio;
using Squ.Character;
using Squ.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "eunuch_script")]
public sealed class EunuchScript : ScriptCardTemplate<EunuchScriptRecord>
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
		SquSfx.Play(SquSfx.EunuchScriptEvent);
		await UnlockScriptAsync(choiceContext);
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(UpgradedDraw - BaseDraw);
	}
}
