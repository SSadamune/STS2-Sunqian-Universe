using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Squ.Audio;
using Squ.Character;
using Squ.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

#nullable enable

namespace Squ.Cards;

[RegisterCard(typeof(SunqianCardPool), StableEntryStem = "portal")]
public sealed class Portal : ModCardTemplate
{
	public const int ScryAmount = 3;
	public const int UpgradedScryAmount = 5;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new ScryVar(ScryAmount),
	];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: "res://images/cards/Portal.png");

	public override IEnumerable<CardKeyword> CanonicalKeywords =>
	[
		SquKeywords.Scry,
	];

	public Portal()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		SquSfx.PlayRandom(
			RunState,
			SquSfx.PortalXiliangAllianceEvent,
			SquSfx.PortalChanganMeiwuEvent,
			SquSfx.PortalJizhouJingzhouEvent,
			SquSfx.PortalGansuHenanEvent,
			SquSfx.PortalYellowRiverRunanEvent);
		await ScryCmd.Execute(choiceContext, this);

		CardModel? drawn = await CardPileCmd.Draw(choiceContext, Owner);
		if (drawn is not { Type: CardType.Attack })
		{
			return;
		}

		// 「对随机目标打出」：AnyEnemy 目标传 null 时会自动随机选敌。
		await CardCmd.AutoPlay(choiceContext, drawn, null);
	}

	protected override void OnUpgrade()
	{
		DynamicVars[ScryVar.VarName].UpgradeValueBy(UpgradedScryAmount - ScryAmount);
	}
}
